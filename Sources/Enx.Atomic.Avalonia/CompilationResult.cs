namespace Enx.Atomic.Avalonia;

/// <summary>
/// Summarizes the outcome of a <see cref="ProjectCompiler{TTheme}.Compile"/> run: which files were scanned,
/// which utility tokens were found in them, and what each configured emitter produced.
/// </summary>
public record CompilationResult
{
    /// <summary>The source files that were scanned for utility tokens.</summary>
    public required IReadOnlyList<SourceFile> ProcessedFiles { get; init; }

    /// <summary>The distinct utility tokens (e.g. <c>"bg-red-500"</c>) extracted from <see cref="ProcessedFiles"/>.</summary>
    public required IReadOnlyList<string> ExtractedTokens { get; init; }

    /// <summary>The output produced by each <see cref="IStyleEmitter{TTheme}"/> configured on the generator.</summary>
    public required IReadOnlyList<EmittedOuput> EmittedOutputs { get; init; }

    /// <summary>How long the compilation took, from extraction through emission.</summary>
    public required TimeSpan Duration { get; init; }
}
