using System.Globalization;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Resolves a dynamic rule's captured value against a theme's scale dictionary (e.g. <see cref="ISpacingPart.Spacing"/>),
/// falling back, in order, to: an <see cref="ArbitraryValue"/> (<c>[123px]</c>, taken as a literal pixel value,
/// no rem conversion — an explicit escape hatch, so it skips the scale entirely rather than trying it first),
/// then a bare <c>rem</c> number — converted to pixels via <see cref="IRemToPxPart.RemToPxFactor"/> — when it
/// isn't a named entry in the scale either.
/// </summary>
internal static class ThemeScale
{
    public static bool TryResolve(this Dictionary<string, float> scale, string key, float remToPxFactor, out float value)
    {
        if (ArbitraryValue.TryUnwrap(key, out var arbitrary))
            return TryParsePx(arbitrary, out value);

        if (scale.TryGetValue(key, out value))
            return true;

        if (float.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out var rem))
        {
            value = rem * remToPxFactor;
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryResolve(this Dictionary<string, double> scale, string key, float remToPxFactor, out double value)
    {
        if (ArbitraryValue.TryUnwrap(key, out var arbitrary))
            return TryParsePx(arbitrary, out value);

        if (scale.TryGetValue(key, out value))
            return true;

        if (double.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out var rem))
        {
            value = rem * remToPxFactor;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Like <see cref="TryResolve(Dictionary{string, float}, string, float, out float)"/>, but falls back to
    /// treating an unmatched raw token as a bare <c>px</c> number instead of <c>rem</c> — matching UnoCSS's
    /// <c>lineWidth</c> scale (border/ring/outline thickness), which resolves unmatched numbers directly in
    /// pixels rather than rem. An <see cref="ArbitraryValue"/> resolves the same way either way.
    /// </summary>
    public static bool TryResolvePx(this Dictionary<string, float> scale, string key, out float value)
    {
        if (ArbitraryValue.TryUnwrap(key, out var arbitrary))
            return TryParsePx(arbitrary, out value);

        if (scale.TryGetValue(key, out value))
            return true;

        if (float.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;

        value = default;
        return false;
    }

    /// <summary>Parses an <see cref="ArbitraryValue"/>'s content as pixels — an optional trailing <c>px</c> unit is stripped, any other unit is rejected (not a supported value).</summary>
    private static bool TryParsePx(string content, out float value)
    {
        var text = content.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? content[..^2] : content;
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <inheritdoc cref="TryParsePx(string, out float)"/>
    private static bool TryParsePx(string content, out double value)
    {
        var text = content.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? content[..^2] : content;
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
