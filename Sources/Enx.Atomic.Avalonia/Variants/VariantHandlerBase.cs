namespace Enx.Atomic.Avalonia;

/// <summary>
/// Produced by a matched <see cref="VariantBase{TTheme}"/>; applies that variant's effect (e.g. wrapping the
/// selector in a pseudo-class, adding a container query) to the style being built. Handlers from all matched
/// variants are composed into a single middleware-style pipeline in <see cref="AtomicGenerator{TTheme}"/>,
/// ordered by <see cref="Order"/>.
/// </summary>
public abstract record VariantHandlerBase
{
    /// <summary>The token to continue matching further variants/rules against after this variant is stripped, or <see langword="null"/> to keep the current token.</summary>
    public string? Matcher { get; init; }

    /// <summary>Relative order this handler runs in within the composed pipeline; lower values run first (outermost).</summary>
    public int Order { get; init; }

    /// <summary>Applies this variant's transformation to <paramref name="input"/>, then calls <paramref name="next"/> to continue the pipeline.</summary>
    public abstract VariantHandlerContext Handle(VariantHandlerContext input,
        Func<VariantHandlerContext, VariantHandlerContext> next);
}
