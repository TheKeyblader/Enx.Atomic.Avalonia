using Avalonia.Controls;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules setting <see cref="TextBlock.TextAlignmentProperty"/>.</summary>
public static class TextAlign
{
    public static readonly Rule.Static[] All =
    [
        new("text-left", [TextBlock.TextAlignmentProperty.ToLiteral(TextAlignment.Left)]),
        new("text-center", [TextBlock.TextAlignmentProperty.ToLiteral(TextAlignment.Center)]),
        new("text-right", [TextBlock.TextAlignmentProperty.ToLiteral(TextAlignment.Right)]),
        new("text-justify", [TextBlock.TextAlignmentProperty.ToLiteral(TextAlignment.Justify)]),
    ];
}
