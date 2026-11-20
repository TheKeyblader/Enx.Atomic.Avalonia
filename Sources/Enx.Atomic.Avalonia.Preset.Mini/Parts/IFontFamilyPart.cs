using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>
/// The font-family scale (<c>font-sans</c>, <c>font-serif</c>, <c>font-mono</c>), mirroring UnoCSS's
/// <c>theme.fontFamily</c> but already resolved to Avalonia <see cref="FontFamily"/> instances: upstream stores one
/// CSS font-family string per key, and Avalonia's <see cref="FontFamily"/> parses the same comma-separated fallback
/// list at construction, so the value is fully known once the theme is built — no rule has to re-parse a string per
/// token.
/// </summary>
public interface IFontFamilyPart
{
    Dictionary<string, FontFamily> FontFamilies { get; set; }
}
