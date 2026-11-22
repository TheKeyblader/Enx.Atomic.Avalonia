using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.CodeGen;
using Enx.Atomic.Avalonia.CodeGen.Values;
using Enx.Atomic.Avalonia.Preset.Mini;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// The strongest check for a C# source emitter: does the text it produces actually compile <em>and produce
/// correctly-typed <see cref="Setter"/>s once run</em>? A source-level check alone misses a real class of bug:
/// <c>Setter</c>'s value parameter is <c>object?</c>, so an emitted numeric literal with the wrong C# type
/// (e.g. <c>448</c>, inferred as <c>int</c>, for a property whose declared type is <c>double</c>) compiles
/// fine — the mismatch only surfaces as a runtime cast failure once Avalonia tries to apply the boxed value.
/// <see cref="EveryRegisteredMiniRule_ProducesCorrectlyTypedValuesBeforeAndAfterEmission"/> covers every rule registration. The targeted fixture covers a mix deliberately chosen
/// to exercise selector and host emission — a single-owner static rule, a
/// multi-owner one (grouping), a color/brush value, a null value (<c>no-underline</c>), a pseudo-class
/// variant (selector chaining), a breakpoint variant (container query), the composite side hosts
/// (<c>ml-1</c>/<c>mr-2</c>, <c>p-2</c>, <c>border-2</c>/<c>border-t-4</c>,
/// <c>rounded-md</c>/<c>rounded-tl-lg</c> write <c>MarginSides</c>/<c>PaddingSides</c>/
/// <c>BorderThicknessSides</c>/<c>CornerRadiusSides</c>' side attached properties, so the generated file must
/// also embed those hosts and their shared recomposition helper),
/// and a whole-number <c>double</c>-valued token (<c>max-w-md</c> — the exact int/double boxing bug above), and
/// a <c>PropertyEquals</c> selector node with an explicit declaring type (<c>dark:bg-red-500</c> — the
/// <c>ThemeVariantScope</c>/<c>AddOwner</c> pitfall documented on <see cref="Enx.Atomic.Avalonia.StyleValue.TargetType"/>),
/// and the <c>ColumnDefinitions</c>/<c>RowDefinitions</c> ghost properties (<c>grid-cols-3</c>), and an
/// arbitrary bracket value of each kind it resolves to — a literal brush (<c>bg-[#123456]</c>) and a literal
/// pixel double (<c>w-[123px]</c>), the <see cref="Avalonia.Media.TextTrimming"/> strategies
/// (<c>truncate</c>/<c>text-ellipsis</c> — a class of static singletons, not an enum), a
/// <see cref="Avalonia.Media.FontFamily"/> value (<c>font-serif</c> — a class reconstructed from its name list),
/// the newer scalar families (<c>size-*</c>, <c>border-x-*</c>, <c>opacity-*</c>, <c>z-*</c>, <c>leading-*</c>,
/// <c>tracking-*</c>), and a <see cref="Avalonia.Controls.Canvas"/> attached property (<c>top-4</c>).
/// </summary>
public class CodeGenTests(ITestOutputHelper output)
{
    [AvaloniaFact]
    public void EveryRegisteredMiniRule_ProducesCorrectlyTypedValuesBeforeAndAfterEmission()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();

        // The documented samples exercise plain resources as well as both variants of an explicitly themed key.
        configuration.Theme.Colors["red-500"] = new Themed<IBrush>(Brushes.Red, Brushes.DarkRed);

        var rules = configuration.Rules;
        var staticTokens = rules.OfType<IStaticRule>().Select(rule => rule.Name).ToArray();
        var tokens = staticTokens
            .Concat(ParityMatrixTests.PortedRuleExampleTokens)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
        var samples = tokens
            .SelectMany(token => generator.ParseToken(token).Select(util => (Token: token, Util: util)))
            .ToArray();

        // Count the actual winning registrations, not regex hits (e.g. text-2 can match a color regex without
        // reaching that rule). Index also distinguishes each static registration and catches shadowed duplicates.
        var covered = samples.Select(sample => sample.Util.Index).ToHashSet();
        var missing = Enumerable
            .Range(0, rules.Count)
            .Where(index => !covered.Contains(index))
            .Select(index => $"#{index} {RuleName(rules[index])}")
            .ToArray();
        Assert.True(
            missing.Length == 0,
            "No sample matched these Mini rule registrations. Add a resolving PARITY.md rule example for each "
                + "uncovered dynamic rule, or check for a shadowed registration:\n"
                + string.Join('\n', missing)
        );

        var themeValues = generator.ResolvedResources.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.ThemeAccess.Compile().DynamicInvoke(configuration.Theme)
        );

        foreach (var (token, util) in samples)
        {
            Assert.NotEmpty(util.Body);
            var context = $"Resolved token '{token}' ({RuleName(rules[util.Index])})";
            foreach (var setter in util.Body)
            {
                if (setter.Value is DynamicResourceExtension resource)
                {
                    var key = Assert.IsType<string>(resource.ResourceKey);
                    Assert.True(themeValues.TryGetValue(key, out var value), $"{context}: missing theme key '{key}'.");
                    if (value is IThemedValue themed)
                    {
                        AssertValueType(setter, themed.LightValue, $"{context}, resource '{key}' (Light)");
                        if (themed.IsThemed)
                            AssertValueType(setter, themed.DarkValue, $"{context}, resource '{key}' (Dark)");
                    }
                    else
                    {
                        AssertValueType(setter, value, $"{context}, resource '{key}'");
                    }
                }
                else
                {
                    AssertValueType(setter, setter.Value, context);
                }
            }
        }

        // Compile the entire sample set once, including the resource dictionary. The emitter adds one root
        // style (possibly a container wrapper) per input util in order, so failures can still name the token.
        var styleSource = StyleEmitter.Emit(
            samples.Select(sample => sample.Util).ToArray(),
            "GeneratedMini",
            "Stylesheet"
        );
        var resourceSource = ResourceDictionaryEmitter.Emit(
            generator.ResolvedResources,
            configuration.Theme,
            "GeneratedMini",
            "Resources"
        );
        var (assembly, errors) = Compile(styleSource, resourceSource);
        Assert.True(errors.Length == 0, string.Join('\n', errors.Select(error => error.ToString())));

        var styles = (Styles)Activator.CreateInstance(assembly!.GetType("GeneratedMini.Stylesheet")!)!;
        var resources = (ResourceDictionary)
            assembly.GetType("GeneratedMini.Resources")!.GetMethod("Build")!.Invoke(null, null)!;
        Assert.Equal(samples.Length, styles.Count);
        for (var index = 0; index < styles.Count; index++)
        {
            var (token, util) = samples[index];
            var context = $"Emitted token '{token}' ({RuleName(rules[util.Index])})";
            var setters = EnumerateSetters([styles[index]]).ToArray();
            Assert.True(
                setters.Length == util.Body.Length,
                $"{context}: emitted setter count differs from resolution."
            );
            foreach (var setter in setters)
            {
                if (setter.Value is DynamicResourceExtension resource)
                {
                    var key = Assert.IsType<string>(resource.ResourceKey);
                    foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
                    {
                        var resourceContext = $"{context}, resource '{key}' ({variant})";
                        Assert.True(
                            resources.TryGetResource(key, variant, out var value),
                            $"{resourceContext}: missing entry."
                        );
                        AssertValueType(setter, value, resourceContext);
                    }
                }
                else
                {
                    AssertValueType(setter, setter.Value, context);
                }
            }
        }

        output.WriteLine(
            $"Checked {covered.Count} rules ({staticTokens.Length} static, {covered.Count - staticTokens.Length} dynamic), "
                + $"{samples.Select(sample => sample.Token).Distinct(StringComparer.Ordinal).Count()} tokens, "
                + $"{samples.Sum(sample => sample.Util.Body.Length)} setters per phase, and {themeValues.Count} resource keys."
        );
    }

    [AvaloniaFact]
    public void EmittedSource_CompilesAndProducesCorrectlyTypedSetters()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var source =
            "Classes=\"ml-1 mr-2 p-2 border-2 border-t-4 rounded-md rounded-tl-lg hidden flex-row cursor-pointer no-underline truncate text-ellipsis hover:bg-red-500 sm:hidden max-w-md dark:bg-red-500 grid-cols-3 col-span-2 bg-[#123456] w-[123px] w-full w-auto shadow-md shadow-inner font-serif size-4 border-x-2 opacity-50 z-10 leading-[20px] tracking-[1px] top-4\"";

        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" });
        Assert.NotEmpty(utils);

        var emitted = StyleEmitter.Emit(utils, "GeneratedStyles", "AtomicStyles");

        // Each family's tokens write an embeddable composite host, so the file must carry one copy of each host
        // (and one shared helper) — and the tokens must stay independent styles (the old ghost combiner merged a
        // pair into one compound `.ml-1.mr-2` style writing a whole Thickness).
        Assert.Contains("file static class MarginSides", emitted);
        Assert.Contains("file static class PaddingSides", emitted);
        Assert.Contains("file static class BorderThicknessSides", emitted);
        Assert.Contains("file static class CornerRadiusSides", emitted);
        Assert.Equal(1, CountOccurrences(emitted, "file static class CompositeSideRecomposition"));
        Assert.Contains("MarginSides.MarginLeftProperty", emitted);
        Assert.Contains("PaddingSides.PaddingTopProperty", emitted);
        Assert.Contains("BorderThicknessSides.BorderThicknessTopProperty", emitted);
        Assert.Contains("CornerRadiusSides.CornerRadiusTopLeftProperty", emitted);
        Assert.DoesNotContain(".Class(\"ml-1\").Class(\"mr-2\")", emitted);

        var (assembly, errors) = Compile(emitted);
        Assert.True(errors.Length == 0, string.Join('\n', errors.Select(e => e.ToString())) + "\n\n" + emitted);

        var stylesType = assembly!.GetType("GeneratedStyles.AtomicStyles")!;
        var styles = (Styles)Activator.CreateInstance(stylesType)!;

        var checkedAnySetter = false;
        foreach (var setter in EnumerateSetters(styles))
        {
            checkedAnySetter = true;
            // Resource payloads are checked by EveryRegisteredMiniRule; this fixture emits no dictionary.
            if (setter.Value is not BindingBase)
                AssertValueType(setter, setter.Value, "Emitted setter");
        }

        Assert.True(checkedAnySetter, "No Setter was found to check — the test source stopped exercising the emitter.");
    }

    /// <summary>
    /// A shortcut reaching codegen through the conditional-class form
    /// (<c>Classes.done="{Binding IsDone}"</c>): the extracted token is the shortcut name, and the emitted
    /// stylesheet must carry that outer class with the shortcut's own variant (<c>:pointerover</c>) attached.
    /// Compiles the output with Roslyn and applies the resulting <see cref="Styles"/> to a shown control, so a
    /// selector that lost the written class (a base class of <c>opacity-50</c> instead of <c>done</c>) or a
    /// variant the emitter dropped fails here rather than only in a source-text check.
    /// </summary>
    [AvaloniaFact]
    public void ShortcutStylesheet_CompilesAndAppliesOnAShownControl()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("done", "line-through hover:opacity-50"));
        var generator = new AtomicGenerator<MiniTheme>(configuration);

        var utils = generator.Generate(
            "Classes.done=\"{Binding IsDone}\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );
        Assert.NotEmpty(utils);

        var emitted = StyleEmitter.Emit(utils, "GeneratedStyles", "AtomicStyles");
        Assert.Contains(".Class(\"done\")", emitted);

        var (assembly, errors) = Compile(emitted);
        Assert.True(errors.Length == 0, string.Join('\n', errors.Select(error => error.ToString())) + "\n\n" + emitted);

        var styles = (Styles)Activator.CreateInstance(assembly!.GetType("GeneratedStyles.AtomicStyles")!)!;
        var text = new TextBlock { Text = "done", Classes = { "done" } };
        var window = new Window { Content = text };
        window.Styles.Add(styles);
        window.Show();
        try
        {
            Assert.Equal(TextDecorations.Strikethrough, text.TextDecorations);
            Assert.Equal(1, text.Opacity);

            text.SetPseudoClass(":pointerover", true);
            Assert.Equal(0.5, text.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SafelistedHoverStyle_CompilesAndOverridesExtractedBaseStyle()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Safelist = ["hover:w-8"];
        var utils = generator.Generate("w-4", new());
        var emitted = StyleEmitter.Emit(utils, "GeneratedStyles", "AtomicStyles");
        var (assembly, errors) = Compile(emitted);
        Assert.True(errors.Length == 0, string.Join('\n', errors.Select(error => error.ToString())) + "\n\n" + emitted);

        var styles = (Styles)Activator.CreateInstance(assembly!.GetType("GeneratedStyles.AtomicStyles")!)!;
        var border = new Border { Classes = { "w-4", "hover:w-8" } };
        var window = new Window { Content = border };
        window.Styles.Add(styles);
        window.Show();
        try
        {
            Assert.Equal(16, border.Width);
            border.SetPseudoClass(":pointerover", true);
            Assert.Equal(32, border.Width);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// <see cref="FontFamily"/> is a class with no round-trippable textual form other than its name list: the emitter
    /// must reproduce a fallback list exactly, special-case the platform default, and refuse a family loaded from a
    /// font asset (whose source can't be rebuilt from its names).
    /// </summary>
    [AvaloniaFact]
    public void FontFamilyEmitter_RoundTripsNamesAndRejectsAssetFamilies()
    {
        var values = new ValueEmitterRegistry();
        var namespaces = new HashSet<string>();

        Assert.Equal(
            "new FontFamily(\"Georgia, Cambria\")",
            values.Emit(new FontFamily("Georgia, Cambria"), namespaces)
        );
        Assert.Contains("Avalonia.Media", namespaces);

        Assert.Equal("FontFamily.Default", values.Emit(FontFamily.Default, new HashSet<string>()));

        Assert.Throws<NotSupportedException>(() =>
            values.Emit(new FontFamily("avares://Tests/Fonts#MyFont"), new HashSet<string>())
        );
    }

    private static string RuleName(IRule rule) =>
        rule is IStaticRule staticRule ? staticRule.Name : rule.GetType().Name.Split('`')[0];

    private static void AssertValueType(Setter setter, object? value, string context)
    {
        Assert.True(setter.Property is not null, $"{context}: setter has no property.");
        var property = setter.Property!;
        var type = property.PropertyType;
        var underlying = Nullable.GetUnderlyingType(type);

        // Setter.Instance calls IsValidValue, which accepts implicit conversions but doesn't store the converted
        // value. EffectiveValue<T>.GetValue later casts the original box to T. Nullable<T> boxes as T or null;
        // reference types (including interfaces, e.g. IBrush) accept assignable instances or null.
        var compatible = value is null
            ? !type.IsValueType || underlying is not null
            : (underlying ?? type).IsInstanceOfType(value);
        Assert.True(
            compatible,
            $"{context}: '{property.OwnerType.Name}.{property.Name}' expects '{type}', "
                + $"but got '{value?.GetType().ToString() ?? "null"}' ({value})."
        );
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var index = haystack.IndexOf(needle, StringComparison.Ordinal); index >= 0; )
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static IEnumerable<Setter> EnumerateSetters(IEnumerable<IStyle> styles)
    {
        foreach (var style in styles)
        {
            if (style is not StyleBase styleBase)
                continue;

            foreach (var setter in styleBase.Setters.OfType<Setter>())
                yield return setter;

            foreach (var setter in EnumerateSetters(styleBase.Children))
                yield return setter;
        }
    }

    private static (Assembly? Assembly, Diagnostic[] Errors) Compile(params string[] sources)
    {
        var trees = sources.Select(
            (source, index) => CSharpSyntaxTree.ParseText(source, path: $"Generated{index}.g.cs")
        );
        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "GeneratedAssembly",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
            return (null, errors);

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
            return (null, [.. result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)]);

        stream.Seek(0, SeekOrigin.Begin);
        return (Assembly.Load(stream.ToArray()), []);
    }
}
