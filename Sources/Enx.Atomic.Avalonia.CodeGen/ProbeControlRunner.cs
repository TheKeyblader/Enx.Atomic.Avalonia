using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Threading;
using Enx.Atomic.Avalonia.Composites;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// The <c>--control</c> half of the probe: instantiate a real Avalonia control, apply the probed styles in
/// exactly their emitted order (container queries at the root of the <c>Styles</c> collection, the control inside
/// a panel registered as container <c>top-level</c> with <see cref="ContainerSizing.Width"/>), merge a resource
/// dictionary built the way <see cref="ResourceDictionaryEmitter"/> builds one, and report the values Avalonia
/// actually ended up with. Uses the headless platform the configuration project already bootstraps — no extra
/// application setup is required beyond <c>Window.Show()</c> + a dispatcher pump.
/// </summary>
internal static class ProbeControlRunner
{
    /// <summary>Runs the control experiment and returns the effective values of every property the styles touched.</summary>
    public static ProbeControlReport Run(
        string typeName,
        double width,
        IReadOnlyList<string> pseudoClasses,
        string containerName,
        IReadOnlyList<string> tokens,
        IReadOnlyList<StringifiedUtil> ordered,
        IReadOnlyList<ProbeResourceInfo> resources
    )
    {
        var type = ResolveControlType(typeName);
        var control =
            Activator.CreateInstance(type) as Control
            ?? throw new InvalidOperationException($"Could not instantiate control type '{typeName}'.");

        var panel = new StackPanel();
        Container.SetName(panel, containerName);
        Container.SetSizing(panel, ContainerSizing.Width);
        panel.Children.Add(control);

        var window = new Window
        {
            Content = panel,
            Width = width,
            Height = 400,
        };

        var dictionary = BuildResourceDictionary(resources);
        if (dictionary.Count > 0)
            window.Resources.MergedDictionaries.Add(dictionary);

        var normalizedPseudoClasses = pseudoClasses
            .Select(NormalizePseudoClass)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var classes = (IPseudoClasses)control.Classes;

        // The probed tokens are the control's style classes, exactly as a consuming app sets Classes="...".
        // Without them every class selector fails to match and the effective values would just be defaults.
        foreach (var token in tokens)
            classes.Add(token);
        foreach (var pseudoClass in normalizedPseudoClasses)
            classes.Add(pseudoClass);

        foreach (var util in ordered)
        {
            var compiledSelector = util.Selector.Compile();
            var style = new Style(selector => compiledSelector(selector!));
            foreach (var setter in util.Body)
                style.Setters.Add(setter);

            if (util.ContainerQuery is null)
            {
                window.Styles.Add(style);
                continue;
            }

            var compiledQuery = util.ContainerQuery.Compile();
            var query = new ContainerQuery(query => compiledQuery(query!), containerName);
            query.Children.Add(style);
            window.Styles.Add(query);
        }

        window.Show();
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();

        var effectiveValues = ReadEffectiveValues(control, ordered);
        window.Close();

        return new ProbeControlReport
        {
            Type = type.FullName ?? type.Name,
            Width = width,
            PseudoClasses = normalizedPseudoClasses,
            ResourceCount = dictionary.Count,
            EffectiveValues = effectiveValues,
        };
    }

    /// <summary>Turns <c>pointerover</c> into <c>:pointerover</c>, so both spellings work on the command line.</summary>
    private static string NormalizePseudoClass(string pseudoClass) =>
        pseudoClass.StartsWith(':') ? pseudoClass : ":" + pseudoClass;

    /// <summary>
    /// Builds the live dictionary the emitted <c>DynamicResource</c> lookups resolve against, mirroring
    /// <see cref="ResourceDictionaryEmitter"/>: a themed value splits across the Light/Dark theme dictionaries,
    /// anything else becomes a plain entry. The values were already resolved once by <see cref="ProbeRunner{TTheme}"/>.
    /// </summary>
    private static ResourceDictionary BuildResourceDictionary(IReadOnlyList<ProbeResourceInfo> resources)
    {
        var dictionary = new ResourceDictionary();
        var light = new ResourceDictionary();
        var dark = new ResourceDictionary();
        var hasThemed = false;

        foreach (var resource in resources)
        {
            if (resource.Resolved is IThemedValue { IsThemed: true } themed)
            {
                hasThemed = true;
                light[resource.Key] = themed.LightValue;
                dark[resource.Key] = themed.DarkValue;
            }
            else
            {
                var value = resource.Resolved is IThemedValue notThemed ? notThemed.LightValue : resource.Resolved;
                dictionary[resource.Key] = value;
            }
        }

        if (hasThemed)
        {
            dictionary.ThemeDictionaries[ThemeVariant.Light] = light;
            dictionary.ThemeDictionaries[ThemeVariant.Dark] = dark;
        }

        return dictionary;
    }

    /// <summary>
    /// Reads every property a probed setter touched, plus the real struct property behind each composite host a
    /// side property belongs to (the recomposed <c>Margin</c>/<c>Padding</c>/<c>BorderThickness</c>/<c>CornerRadius</c>).
    /// </summary>
    private static ProbeEffectiveValueReport[] ReadEffectiveValues(
        Control control,
        IReadOnlyList<StringifiedUtil> ordered
    )
    {
        var properties = new List<AvaloniaProperty>();
        var seen = new HashSet<AvaloniaProperty>();
        var resourceKeys = new Dictionary<AvaloniaProperty, string>();

        foreach (var util in ordered)
        {
            foreach (var setter in util.Body)
            {
                if (setter.Property is not { } property)
                    continue;

                if (seen.Add(property))
                    properties.Add(property);

                if (setter.Value is DynamicResourceExtension dynamicResource)
                    resourceKeys[property] = dynamicResource.ResourceKey?.ToString() ?? string.Empty;
            }
        }

        var entries = new List<ProbeEffectiveValueReport>();
        var compositeSides = new Dictionary<CompositePropertyDescriptor, List<AvaloniaProperty>>();

        foreach (var property in properties)
        {
            var registered = AvaloniaPropertyRegistry.Instance.IsRegistered(control, property);
            var value = registered ? control.GetValue(property) : null;

            entries.Add(
                new ProbeEffectiveValueReport
                {
                    Property = ProbeValues.Property(property),
                    Value = registered ? ProbeValues.Value(value) : "(not registered on this control)",
                    ValueType = value?.GetType().FullName,
                    Registered = registered,
                    ResourceKey = resourceKeys.GetValueOrDefault(property),
                }
            );

            var descriptor = FindCompositeDescriptor(property);
            if (descriptor is null)
                continue;

            if (!compositeSides.TryGetValue(descriptor, out var sides))
                compositeSides[descriptor] = sides = [];
            sides.Add(property);
        }

        foreach (var (descriptor, sides) in compositeSides)
        {
            var realProperty = descriptor.RealProperty;
            if (!AvaloniaPropertyRegistry.Instance.IsRegistered(control, realProperty))
                continue;

            var value = control.GetValue(realProperty);
            entries.Add(
                new ProbeEffectiveValueReport
                {
                    Property = ProbeValues.Property(realProperty),
                    Value = ProbeValues.Value(value),
                    ValueType = value?.GetType().FullName,
                    Registered = true,
                    ComposedFrom = [.. sides.Select(ProbeValues.Property)],
                }
            );
        }

        return [.. entries];
    }

    /// <summary>
    /// Finds the composite host a side property belongs to, by looking for the public static <c>Descriptor</c>
    /// a host declares (see <see cref="CompositePropertyDescriptor"/>) and checking the property is one of its
    /// four registered side names. Works for a user-defined host as well as the shipped four.
    /// </summary>
    private static CompositePropertyDescriptor? FindCompositeDescriptor(AvaloniaProperty property) =>
        property.OwnerType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            is CompositePropertyDescriptor descriptor
        && descriptor.AttachedPropertyNames.Contains(property.Name)
            ? descriptor
            : null;

    /// <summary>
    /// Resolves a simple or full type name against the loaded Avalonia assemblies. The assemblies are
    /// enumerated rather than a fixed list (Avalonia.Controls, Avalonia.Controls.Primitives, ...) so the command
    /// keeps working for any control package a consuming app has loaded.
    /// </summary>
    private static Type ResolveControlType(string typeName)
    {
        var assemblies = new List<Assembly>(
            AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic)
        );

        foreach (var assemblyName in (ReadOnlySpan<string>)["Avalonia.Controls", "Avalonia"])
        {
            try
            {
                var assembly = Assembly.Load(assemblyName);
                if (!assemblies.Contains(assembly))
                    assemblies.Add(assembly);
            }
            catch (FileNotFoundException)
            {
                // The assembly isn't deployed — nothing to search.
            }
        }

        var matches = new List<Type>();
        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = [.. exception.Types.Where(type => type is not null).Cast<Type>()];
            }

            matches.AddRange(
                types.Where(type =>
                    (type.Name == typeName || type.FullName == typeName)
                    && typeof(Control).IsAssignableFrom(type)
                    && !type.IsAbstract
                    && !type.IsGenericTypeDefinition
                    && type.GetConstructor(Type.EmptyTypes) is not null
                )
            );
        }

        return matches.OrderBy(type => type.FullName, StringComparer.Ordinal).FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"No instantiable Avalonia control named '{typeName}' was found in the loaded assemblies. "
                    + "Pass a simple name such as Button, Border or TextBlock, or a full type name."
            );
    }
}
