namespace Enx.Atomic.Avalonia;

public abstract record VariantBase<TTheme>
    where TTheme : class
{
    public int Order { get; set; }
    public bool MultiPass { get; set; }

    public abstract VariantHandlerBase[] Matcher(string matcher, VariantContext<TTheme> context);
}
