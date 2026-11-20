using System.Linq.Expressions;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.CodeGen;
using Enx.Atomic.Avalonia.Compact;
using Enx.Atomic.Avalonia.Composites;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>Which of the four shipped composites a test is talking about.</summary>
public enum CompositeGroup
{
    Margin,
    Padding,
    BorderThickness,
    CornerRadius,
}

/// <summary>
/// The four side attached properties of one composite host, in slot order. <see cref="Slot0"/>..<see cref="Slot3"/>
/// mean Left/Top/Right/Bottom for a <see cref="CompositeKind.Thickness"/> host and
/// TopLeft/TopRight/BottomRight/BottomLeft for <see cref="CompositeKind.CornerRadius"/> — see
/// <see cref="CompositeKind"/>.
/// </summary>
public sealed record SideProperties(
    AttachedProperty<double> Slot0,
    AttachedProperty<double> Slot1,
    AttachedProperty<double> Slot2,
    AttachedProperty<double> Slot3
)
{
    public AttachedProperty<double>[] ToArray() => [Slot0, Slot1, Slot2, Slot3];
}

/// <summary>The real (non-embedded) hosts, reached statically — touching any member runs that host's static constructor.</summary>
internal static class RuntimeCompositeHosts
{
    public static CompositePropertyDescriptor Descriptor(CompositeGroup group) =>
        group switch
        {
            CompositeGroup.Margin => MarginSides.Descriptor,
            CompositeGroup.Padding => PaddingSides.Descriptor,
            CompositeGroup.BorderThickness => BorderThicknessSides.Descriptor,
            CompositeGroup.CornerRadius => CornerRadiusSides.Descriptor,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null),
        };

    public static Type HostType(CompositeGroup group) =>
        group switch
        {
            CompositeGroup.Margin => typeof(MarginSides),
            CompositeGroup.Padding => typeof(PaddingSides),
            CompositeGroup.BorderThickness => typeof(BorderThicknessSides),
            CompositeGroup.CornerRadius => typeof(CornerRadiusSides),
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null),
        };

    public static SideProperties Sides(CompositeGroup group) =>
        group switch
        {
            CompositeGroup.Margin => new(
                MarginSides.MarginLeftProperty,
                MarginSides.MarginTopProperty,
                MarginSides.MarginRightProperty,
                MarginSides.MarginBottomProperty
            ),
            CompositeGroup.Padding => new(
                PaddingSides.PaddingLeftProperty,
                PaddingSides.PaddingTopProperty,
                PaddingSides.PaddingRightProperty,
                PaddingSides.PaddingBottomProperty
            ),
            CompositeGroup.BorderThickness => new(
                BorderThicknessSides.BorderThicknessLeftProperty,
                BorderThicknessSides.BorderThicknessTopProperty,
                BorderThicknessSides.BorderThicknessRightProperty,
                BorderThicknessSides.BorderThicknessBottomProperty
            ),
            CompositeGroup.CornerRadius => new(
                CornerRadiusSides.CornerRadiusTopLeftProperty,
                CornerRadiusSides.CornerRadiusTopRightProperty,
                CornerRadiusSides.CornerRadiusBottomRightProperty,
                CornerRadiusSides.CornerRadiusBottomLeftProperty
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null),
        };

    public static bool EnsureInitialized()
    {
        // Reading the descriptors is a static field access, which is all it takes to guarantee every host's
        // static constructor (property registration + class handlers) ran first.
        _ = MarginSides.Descriptor;
        _ = PaddingSides.Descriptor;
        _ = BorderThicknessSides.Descriptor;
        _ = CornerRadiusSides.Descriptor;
        return true;
    }
}

/// <summary>Builds the data the emitter consumes, and compiles its output for real — the same pattern as <c>CodeGenTests</c>.</summary>
internal static class CompositeTestSource
{
    /// <summary>Builds a resolved util with a class selector and one setter per entry, in the given order.</summary>
    public static StringifiedUtil StyleUtil(
        string className,
        int index,
        params (AttachedProperty<double> Property, double Value)[] setters
    )
    {
        var selectorData = SelectorsExpression.Is<Control>(null).Class(className);
        var parameter = Expression.Parameter(typeof(Selector), "selector");
        return new StringifiedUtil
        {
            Index = index,
            Selector = Expression.Lambda<Func<Selector, Selector>>(selectorData.ToExpression(parameter), parameter),
            SelectorData = selectorData,
            Body = [.. setters.Select(setter => new Setter(setter.Property, setter.Value))],
            Metadata = new RuleMetadata(),
        };
    }

    public static (Assembly? Assembly, Diagnostic[] Errors) Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        // Deliberately excludes every Enx.Atomic assembly: the embedded hosts must compile and run against
        // Avalonia alone, since a consuming project that only takes the generated output may not reference the
        // engine or the preset at all.
        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Where(a => !a.GetName().Name!.StartsWith("Enx.Atomic", StringComparison.Ordinal))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "CompositeHostProbeAssembly",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
            return (null, [.. result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)]);

        stream.Seek(0, SeekOrigin.Begin);
        return (Assembly.Load(stream.ToArray()), []);
    }

    public static IEnumerable<Setter> EnumerateSetters(IEnumerable<IStyle> styles)
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
}

/// <summary>
/// The embedded copy of the four composite hosts, produced exactly the way the build-time generator produces it:
/// hand-built <see cref="StringifiedUtil"/>s targeting the real hosts → <see cref="StyleEmitter.Emit"/> →
/// Roslyn compile → load into this test process. The scenario suite runs against the property instances taken
/// from the compiled file's own setters, so every behavioral assertion exercises the embedded source, not the
/// runtime host.
/// </summary>
internal sealed class EmbeddedCompositeHosts
{
    private static readonly CompositeGroup[] Groups =
    [
        CompositeGroup.Margin,
        CompositeGroup.Padding,
        CompositeGroup.BorderThickness,
        CompositeGroup.CornerRadius,
    ];

    private static readonly Lazy<EmbeddedCompositeHosts> LazyInstance = new(Load);

    private readonly Type _stylesType;
    private readonly Dictionary<CompositeGroup, SideProperties> _sides;

    private EmbeddedCompositeHosts(Type stylesType, Dictionary<CompositeGroup, SideProperties> sides, string source)
    {
        _stylesType = stylesType;
        _sides = sides;
        Source = source;
    }

    public static EmbeddedCompositeHosts Instance => LazyInstance.Value;

    /// <summary>The full generated file, for assertions about what the emitter embedded.</summary>
    public string Source { get; }

    public SideProperties Sides(CompositeGroup group) => _sides[group];

    /// <summary>A fresh, unattached instance of the generated <c>Styles</c> class.</summary>
    public Styles CreateStyles() => (Styles)Activator.CreateInstance(_stylesType)!;

    private static EmbeddedCompositeHosts Load()
    {
        var utils = new List<StringifiedUtil>();
        var index = 0;
        foreach (var group in Groups)
        {
            // The real hosts only supply the property instances the emitter reads (owner type, field name) to
            // build the input; every assertion in the scenario suite uses the instances then extracted from the
            // compiled file's own setters, i.e. the embedded copy.
            var sides = RuntimeCompositeHosts.Sides(group);
            // Deliberately distinct values per slot: a swapped slot order then changes the asserted struct.
            var values = group switch
            {
                CompositeGroup.Margin => (1d, 2d, 3d, 4d),
                CompositeGroup.Padding => (5d, 6d, 7d, 8d),
                CompositeGroup.BorderThickness => (9d, 10d, 11d, 12d),
                CompositeGroup.CornerRadius => (13d, 14d, 15d, 16d),
                _ => throw new ArgumentOutOfRangeException(nameof(group), group, null),
            };

            utils.Add(
                CompositeTestSource.StyleUtil(
                    $"probe-{group}",
                    index++,
                    (sides.Slot0, values.Item1),
                    (sides.Slot1, values.Item2),
                    (sides.Slot2, values.Item3),
                    (sides.Slot3, values.Item4)
                )
            );
        }

        var source = StyleEmitter.Emit(utils, "CompositeHostProbe", "ProbeStyles");
        var (assembly, errors) = CompositeTestSource.Compile(source);
        Assert.True(errors.Length == 0, string.Join('\n', errors.Select(error => error.ToString())) + "\n\n" + source);

        var stylesType = assembly!.GetType("CompositeHostProbe.ProbeStyles")!;
        var probeStyles = (Styles)Activator.CreateInstance(stylesType)!;

        var byName = new Dictionary<string, AttachedProperty<double>>(StringComparer.Ordinal);
        foreach (var setter in CompositeTestSource.EnumerateSetters(probeStyles))
        {
            if (setter.Property is AttachedProperty<double> property)
                byName[property.Name] = property;
        }

        Assert.Equal(Groups.Length * 4, byName.Count);

        var sidesByGroup = Groups.ToDictionary(
            group => group,
            group =>
            {
                var names = RuntimeCompositeHosts.Descriptor(group).AttachedPropertyNames;
                return new SideProperties(byName[names[0]], byName[names[1]], byName[names[2]], byName[names[3]]);
            }
        );

        return new EmbeddedCompositeHosts(stylesType, sidesByGroup, source);
    }
}
