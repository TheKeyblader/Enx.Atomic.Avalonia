namespace Enx.Atomic.Avalonia;

/// <summary>
/// The cascade layers <see cref="StringifiedUtil.Layer"/> distinguishes. Numeric and ascending, like UnoCSS's
/// <c>shortcuts</c> layer emitted before <c>default</c>: a shortcut's styles are emitted first, so a plain utility
/// with the same specificity wins the Avalonia frame-order tie. Specificity is compared before the layer, so a
/// more specific selector (e.g. a shortcut used as <c>hover:btn</c>) still beats a plain utility.
/// </summary>
public static class StyleLayer
{
    /// <summary>Styles expanded from a shortcut token.</summary>
    public const int Shortcuts = 0;

    /// <summary>Styles resolved from a plain utility token, and the default for a util built outside token resolution.</summary>
    public const int Default = 1;
}
