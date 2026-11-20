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
    public Dictionary<string, double> Spacing { get; set; } = [];
    public Dictionary<string, double> Sizes { get; set; } = [];
    public Dictionary<string, double> Radii { get; set; } = [];
    public Dictionary<string, Themed<IBrush>> Colors { get; set; } = [];
    public Dictionary<string, double> FontSizes { get; set; } = [];
    public Dictionary<string, double> LineWidths { get; set; } = [];
    public Dictionary<string, double> Breakpoints { get; set; } = [];
    public double RemToPxFactor { get; set; }
}
