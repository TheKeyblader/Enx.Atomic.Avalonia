using Microsoft.Extensions.FileSystemGlobbing;

namespace Enx.Atomic.Avalonia;

public class ProjectCompiler<TTheme>(
    AtomicGenerator<TTheme> Generator,
    IStyleEmitter<TTheme> Emitter,
    ProjectCompilerOptions Options
)
    where TTheme : class
{
    public CompilationResult Compile()
    {
        var start = DateTime.UtcNow;

        var matcher = new Matcher();
        matcher.AddIncludePatterns(Options.IncludePatterns);
        matcher.AddExcludePatterns(Options.ExcludePatterns);
    }
}
