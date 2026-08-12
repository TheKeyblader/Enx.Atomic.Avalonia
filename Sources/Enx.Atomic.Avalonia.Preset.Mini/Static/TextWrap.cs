using Avalonia.Controls;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>
/// Static rules setting <see cref="TextBlock.TextWrappingProperty"/> and <see cref="TextBlock.TextTrimmingProperty"/>.
/// </summary>
public static class TextWrap
{
    public static readonly Rule.Static[] All =
    [
        new("whitespace-nowrap", [TextBlock.TextWrappingProperty.ToLiteral(TextWrapping.NoWrap)]),
        new("whitespace-normal", [TextBlock.TextWrappingProperty.ToLiteral(TextWrapping.Wrap)]),
        new("text-clip", [TextBlock.TextTrimmingProperty.ToLiteral(TextTrimming.None)]),
        new("text-ellipsis", [TextBlock.TextTrimmingProperty.ToLiteral(TextTrimming.CharacterEllipsis)]),
        new(
            "truncate",
            [
                TextBlock.TextWrappingProperty.ToLiteral(TextWrapping.NoWrap),
                TextBlock.TextTrimmingProperty.ToLiteral(TextTrimming.CharacterEllipsis),
            ]
        ),
    ];
}
