namespace Enx.Atomic.Avalonia;

/// <summary>
/// A variant strips a prefix or suffix from a utility token (e.g. <c>hover:</c>, <c>dark:</c>) and contributes
/// a <see cref="VariantHandlerBase"/> that later transforms the resolved style's selector, container query
/// or entries. <see cref="AtomicGenerator{TTheme}.MatchVariants(string, string?)"/> tries every configured
/// variant against the current token on each pass, in <see cref="Order"/>, until none match.
/// </summary>
public abstract record VariantBase<TTheme>
    where TTheme : class
{
    /// <summary>Relative order variants are tried in during a single matching pass; lower values are tried first.</summary>
    public int Order { get; set; }

    /// <summary>
    /// Whether this variant may match more than once against the same token across passes. Non-multi-pass
    /// variants are only tried once per token, even across recursive matches.
    /// </summary>
    public bool MultiPass { get; set; }

    /// <summary>
    /// Attempts to match this variant against <paramref name="matcher"/>. Returns one handler per successful
    /// interpretation, or an empty array if the variant does not apply.
    /// </summary>
    public abstract VariantHandlerBase[] Match(string matcher, VariantContext<TTheme> context);
}
