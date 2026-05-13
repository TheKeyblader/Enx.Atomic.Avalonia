namespace Enx.Atomic.Avalonia;

public record EmitContext<TTheme>
    where TTheme : class
{
    public required AtomicConfiguration<TTheme> Configuration { get; init; }
    public required StringifiedUtil[] Utils { get; init; }
}