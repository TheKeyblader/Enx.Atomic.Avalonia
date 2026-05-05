namespace Enx.Atomic.Avalonia;

public record RuleContext<TTheme>
    where TTheme : class
{
    public required string RawSelector { get; init; }
    public required string CurrentSelector { get; init; }
    public required AtomicGenerator<TTheme> Generator { get; init; }
    public required TTheme Theme { get; init; }
    public required VariantHandlerBase[] Handlers { get; init; }
    public required VariantMatchedResult<TTheme> Match { get; init; }
}