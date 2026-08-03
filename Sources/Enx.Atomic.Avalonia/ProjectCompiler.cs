using Microsoft.Extensions.FileSystemGlobbing;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Scans a set of project files for utility tokens, resolves them with an <see cref="AtomicGenerator{TTheme}"/>,
/// and hands the resulting styles to an <see cref="IStyleEmitter{TTheme}"/> — the end-to-end pipeline for
/// producing atomic Avalonia styles from source code.
/// </summary>
/// <param name="Generator">Resolves extracted utility tokens into styles.</param>
/// <param name="Emitter">Emits the resolved styles to their final form (e.g. generated C# or a runtime resource dictionary).</param>
/// <param name="Options">Where to find source files and where to write output.</param>
public class ProjectCompiler<TTheme>(
    AtomicGenerator<TTheme> Generator,
    IStyleEmitter<TTheme> Emitter,
    ProjectCompilerOptions Options
)
    where TTheme : class
{
    /// <summary>Runs the full scan-extract-generate-emit pipeline over the configured project files.</summary>
    public CompilationResult Compile()
    {
        var start = DateTime.UtcNow;

        var matcher = new Matcher();
        matcher.AddIncludePatterns(Options.IncludePatterns);
        matcher.AddExcludePatterns(Options.ExcludePatterns);
    }
}
