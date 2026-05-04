namespace Enx.Atomic.Avalonia;

public record VariantContext<TTheme>
    where TTheme : class
{
    public required TTheme Theme { get; set; }
}
