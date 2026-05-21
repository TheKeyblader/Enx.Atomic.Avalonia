namespace Enx.Atomic.Avalonia;

public abstract record VariantHandlerBase
{
    public string? Matcher { get; init; }
    public int Order { get; init; }
    public abstract VariantHandlerContext Handle(VariantHandlerContext input,
        Func<VariantHandlerContext, VariantHandlerContext> next);
}
