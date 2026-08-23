using System.Globalization;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Resolves a dynamic rule's captured value against a theme's scale dictionary (e.g. <see cref="ISpacingPart.Spacing"/>),
/// falling back to treating the raw token as a bare <c>rem</c> number — converted to pixels via
/// <see cref="IRemToPxPart.RemToPxFactor"/> — when it isn't a named entry in the scale.
/// </summary>
internal static class ThemeScale
{
    public static bool TryResolve(this Dictionary<string, float> scale, string key, float remToPxFactor, out float value)
    {
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
    /// treating the raw token as a bare <c>px</c> number instead of <c>rem</c> — matching UnoCSS's <c>lineWidth</c>
    /// scale (border/ring/outline thickness), which resolves unmatched numbers directly in pixels rather than rem.
    /// </summary>
    public static bool TryResolvePx(this Dictionary<string, float> scale, string key, out float value)
    {
        if (scale.TryGetValue(key, out value))
            return true;

        if (float.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;

        value = default;
        return false;
    }
}
