namespace Enx.Atomic.Avalonia;

/// <summary>The context a <see cref="VariantBase{TTheme}"/> receives while attempting to match a token.</summary>
public record VariantContext<TTheme>
    where TTheme : class
{
    /// <summary>The original, unstripped token being matched.</summary>
    public required string RawSelector { get; init; }

    /// <summary>The generator performing the match.</summary>
    public required AtomicGenerator<TTheme> Generator { get; init; }

    /// <summary>The active theme instance.</summary>
    public required TTheme Theme { get; set; }
}
