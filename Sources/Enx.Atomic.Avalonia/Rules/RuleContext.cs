namespace Enx.Atomic.Avalonia;

/// <summary>The context a <see cref="Rule"/> receives while resolving a token, giving it access to the theme, the generator, and the variants already stripped.</summary>
public record RuleContext<TTheme>
    where TTheme : class
{
    /// <summary>The original, unstripped token.</summary>
    public required string RawSelector { get; init; }

    /// <summary>The token after variant stripping — what the rule actually matches against.</summary>
    public required string CurrentSelector { get; init; }

    /// <summary>The generator resolving this token, for rules that need to recurse (e.g. compound utilities).</summary>
    public required AtomicGenerator<TTheme> Generator { get; init; }

    /// <summary>The active theme instance.</summary>
    public required TTheme Theme { get; init; }

    /// <summary>The variant handlers matched before rule resolution, in match order.</summary>
    public required VariantHandlerBase[] Handlers { get; init; }

    /// <summary>The full variant match result this context was derived from.</summary>
    public required VariantMatchedResult<TTheme> Match { get; init; }
}