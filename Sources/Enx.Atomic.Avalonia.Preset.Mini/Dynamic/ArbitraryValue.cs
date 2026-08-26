namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// UnoCSS/Tailwind's bracket syntax for a one-off value that bypasses a rule's theme scale entirely
/// (<c>bg-[#ff0000]</c>, <c>w-[123px]</c>) — an escape hatch for a value that doesn't belong in, or isn't worth
/// naming in, the theme. Always resolves to a <see cref="StyleValue.Literal{TValue}"/>, never a
/// <see cref="StyleValue.Resource"/>: unlike a named scale entry, an arbitrary value has no theme member for a
/// <c>ThemeAccess</c> expression to point at.
/// </summary>
internal static class ArbitraryValue
{
    /// <summary>
    /// Strips the surrounding <c>[...]</c> from <paramref name="raw"/> into <paramref name="content"/>, with
    /// underscores turned back into spaces (spaces can't appear in a class token, so UnoCSS/Tailwind's bracket
    /// syntax uses <c>_</c> as a stand-in — e.g. a future <c>grid-cols-[200px_1fr]</c>).
    /// </summary>
    public static bool TryUnwrap(string raw, out string content)
    {
        if (raw.Length > 2 && raw[0] == '[' && raw[^1] == ']')
        {
            content = raw[1..^1].Replace('_', ' ');
            return true;
        }

        content = string.Empty;
        return false;
    }
}
