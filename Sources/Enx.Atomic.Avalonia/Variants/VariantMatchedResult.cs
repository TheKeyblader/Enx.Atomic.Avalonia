namespace Enx.Atomic.Avalonia;

/// <summary>The accumulated result of matching variants against a token in <see cref="AtomicGenerator{TTheme}.MatchVariants(string, string?)"/>.</summary>
public record VariantMatchedResult<TTheme>
    where TTheme : class
{
    /// <summary>The original, unstripped token.</summary>
    public required string Raw { get; init; }

    /// <summary>The token with all matched variants stripped, ready for rule matching.</summary>
    public required string Current { get; init; }

    /// <summary>The handlers contributed by each matched variant, in match order.</summary>
    public VariantHandlerBase[] Handlers { get; init; } = [];

    /// <summary>The set of variants already matched, used to prevent non-multi-pass variants from matching twice.</summary>
    public HashSet<VariantBase<TTheme>> Variants { get; init; } = [];
}
