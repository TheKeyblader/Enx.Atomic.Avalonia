using Enx.Atomic.Avalonia.CodeGen;
using Enx.Atomic.Avalonia.Preset.Mini;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// The strongest check for a C# source emitter: does the text it produces actually compile? Covers a mix
/// deliberately chosen to exercise most of the emitter — a single-owner static rule, a multi-owner one
/// (grouping), a color/brush value, a null value (<c>no-underline</c>), a pseudo-class variant (selector
/// chaining), a breakpoint variant (container query), and ghost-property combining (<c>SpecialProperties</c>
/// owner type, a compound selector via <see cref="GhostPropertyCombiner{TTheme}"/>).
/// </summary>
public class CodeGenTests
{
    [AvaloniaFact]
    public void EmittedSource_CompilesWithoutErrors()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var source = "Classes=\"ml-1 mr-2 hidden flex-row cursor-pointer no-underline hover:bg-red-500 sm:hidden\"";

        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" });
        Assert.NotEmpty(utils);

        var emitted = StyleEmitter.Emit(utils, "GeneratedStyles", "AtomicStyles");

        var errors = Compile(emitted).Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, string.Join('\n', errors.Select(e => e.ToString())) + "\n\n" + emitted);
    }

    private static Diagnostic[] Compile(string source)
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

        return [.. compilation.GetDiagnostics()];
    }
}
