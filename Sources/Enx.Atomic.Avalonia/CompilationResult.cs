namespace Enx.Atomic.Avalonia;

public record CompilationResult
{
    public required IReadOnlyList<SourceFile> ProcessedFiles { get; init; }
    public required IReadOnlyList<string> ExtractedTokens { get; init; }
    public required IReadOnlyList<EmittedOuput> EmittedOutputs { get; init; }
    public required TimeSpan Duration { get; init; }
}
