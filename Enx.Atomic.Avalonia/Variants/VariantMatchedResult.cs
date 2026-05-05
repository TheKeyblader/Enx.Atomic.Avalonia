namespace Enx.Atomic.Avalonia;

public record VariantMatchedResult<TTheme>
    where TTheme : class
{
    public required string Raw { get; init; }
    public required string Current { get; init; }
    public VariantHandlerBase[] Handlers { get; init; } = [];
    public HashSet<VariantBase<TTheme>> Variants { get; init; } = [];
}
