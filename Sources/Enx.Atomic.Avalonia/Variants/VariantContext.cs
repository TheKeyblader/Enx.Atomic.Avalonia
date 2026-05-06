namespace Enx.Atomic.Avalonia;

public record VariantContext<TTheme>
    where TTheme : class
{
    public required string RawSelector { get; init; }
    public required AtomicGenerator<TTheme> Generator { get; init; }
    public required TTheme Theme { get; set; }
}
