using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>The theme backing the Mini preset's dynamic rules, aggregating every <c>Parts/</c> contract they depend on.</summary>
public class MiniTheme
    : ISpacingPart,
        ISizePart,
        IRadiusPart,
        IColorPart,
        IFontSizePart,
        ILineWidthPart,
        IBreakpointPart,
        IRemToPxPart
{
    public Dictionary<string, float> Spacing { get; set; } = [];
    public Dictionary<string, float> Sizes { get; set; } = [];
    public Dictionary<string, float> Radii { get; set; } = [];
    public Dictionary<string, Themed<IBrush>> Colors { get; set; } = [];
    public Dictionary<string, double> FontSizes { get; set; } = [];
    public Dictionary<string, float> LineWidths { get; set; } = [];
    public Dictionary<string, double> Breakpoints { get; set; } = [];
    public float RemToPxFactor { get; set; }
}
