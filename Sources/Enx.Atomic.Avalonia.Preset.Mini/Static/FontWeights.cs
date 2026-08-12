using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules setting <see cref="TextElement.FontWeightProperty"/>.</summary>
public static class FontWeights
{
    public static readonly Rule.Static[] All =
    [
        new("font-thin", [TextElement.FontWeightProperty.ToLiteral(FontWeight.Thin)]),
        new("font-extralight", [TextElement.FontWeightProperty.ToLiteral(FontWeight.ExtraLight)]),
        new("font-light", [TextElement.FontWeightProperty.ToLiteral(FontWeight.Light)]),
        new("font-normal", [TextElement.FontWeightProperty.ToLiteral(FontWeight.Normal)]),
        new("font-medium", [TextElement.FontWeightProperty.ToLiteral(FontWeight.Medium)]),
        new("font-semibold", [TextElement.FontWeightProperty.ToLiteral(FontWeight.SemiBold)]),
        new("font-bold", [TextElement.FontWeightProperty.ToLiteral(FontWeight.Bold)]),
        new("font-extrabold", [TextElement.FontWeightProperty.ToLiteral(FontWeight.ExtraBold)]),
        new("font-black", [TextElement.FontWeightProperty.ToLiteral(FontWeight.Black)]),
    ];
}
