using System.Reflection;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.CodeGen;
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
/// Covers a mix deliberately chosen to exercise most of the emitter — a single-owner static rule, a
/// multi-owner one (grouping), a color/brush value, a null value (<c>no-underline</c>), a pseudo-class
/// variant (selector chaining), a breakpoint variant (container query), ghost-property combining
/// (<c>SpecialProperties</c> owner type, a compound selector via <see cref="GhostPropertyCombiner{TTheme}"/>),
/// and a whole-number <c>double</c>-valued token (<c>max-w-md</c> — the exact int/double boxing bug above), and
/// a <c>PropertyEquals</c> selector node with an explicit declaring type (<c>dark:bg-red-500</c> — the
/// <c>ThemeVariantScope</c>/<c>AddOwner</c> pitfall documented on <see cref="Enx.Atomic.Avalonia.StyleValue.TargetType"/>).
/// </summary>
public class CodeGenTests
{
    [AvaloniaFact]
    public void EmittedSource_CompilesAndProducesCorrectlyTypedSetters()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var source =
            "Classes=\"ml-1 mr-2 hidden flex-row cursor-pointer no-underline hover:bg-red-500 sm:hidden max-w-md dark:bg-red-500\"";

        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" });
        Assert.NotEmpty(utils);

        var emitted = StyleEmitter.Emit(utils, "GeneratedStyles", "AtomicStyles");

        var (assembly, errors) = Compile(emitted);
        Assert.True(errors.Length == 0, string.Join('\n', errors.Select(e => e.ToString())) + "\n\n" + emitted);

        var stylesType = assembly!.GetType("GeneratedStyles.AtomicStyles")!;
        var styles = (Styles)Activator.CreateInstance(stylesType)!;

        var checkedAnySetter = false;
        foreach (var setter in EnumerateSetters(styles))
        {
            checkedAnySetter = true;
            var propertyType = setter.Property!.PropertyType;
            Assert.True(
                setter.Value is null ? propertyType.IsClass || Nullable.GetUnderlyingType(propertyType) != null
                    : propertyType.IsInstanceOfType(setter.Value),
                $"Setter for '{setter.Property}' expects '{propertyType}' but got a boxed '{setter.Value?.GetType().ToString() ?? "null"}' ({setter.Value})."
            );
        }

        Assert.True(checkedAnySetter, "No Setter was found to check — the test source stopped exercising the emitter.");
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

    private static (Assembly? Assembly, Diagnostic[] Errors) Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "GeneratedAssembly",
            [tree],
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
