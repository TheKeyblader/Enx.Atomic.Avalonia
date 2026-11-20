namespace Enx.Atomic.Avalonia.Shortcuts;

/// <summary>
/// Splits and normalizes a shortcut's expansion, shared by both shortcut shapes so a static and a dynamic
/// shortcut with the same textual expansion resolve to exactly the same token list.
/// </summary>
internal static class ShortcutExpansion
{
    /// <summary>Splits on whitespace, trims, drops empty entries and keeps the first occurrence of each token.</summary>
    public static string[] Split(string expansion) =>
        [
            .. expansion
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal),
        ];

    /// <summary>Splits every string and concatenates the results, then keeps the first occurrence of each token.</summary>
    public static string[] Split(IEnumerable<string> expansions) =>
        [.. expansions.SelectMany(Split).Distinct(StringComparer.Ordinal)];
}
