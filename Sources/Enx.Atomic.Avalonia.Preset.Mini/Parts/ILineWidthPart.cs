namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>The line-width scale (border/ring/outline thickness), kept separate from <see cref="ISpacingPart.Spacing"/> — mirrors UnoCSS's <c>theme.lineWidth</c>, which uses a small px-based scale distinct from the larger rem-based spacing scale.</summary>
public interface ILineWidthPart
{
    Dictionary<string, float> LineWidths { get; set; }
}
