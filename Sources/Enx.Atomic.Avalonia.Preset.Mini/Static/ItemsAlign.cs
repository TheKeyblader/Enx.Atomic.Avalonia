using Avalonia.Controls;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>
/// Static rules setting <see cref="ContentControl.HorizontalContentAlignmentProperty"/> (<c>justify-items-*</c>)
/// and <see cref="ContentControl.VerticalContentAlignmentProperty"/> (<c>items-*</c>) — how a content control
/// aligns its content by default.
/// </summary>
public static class ItemsAlign
{
    public static readonly Rule.Static[] All =
    [
        new(
            "justify-items-start",
            [ContentControl.HorizontalContentAlignmentProperty.ToLiteral(HorizontalAlignment.Left)]
        ),
        new(
            "justify-items-center",
            [ContentControl.HorizontalContentAlignmentProperty.ToLiteral(HorizontalAlignment.Center)]
        ),
        new(
            "justify-items-end",
            [ContentControl.HorizontalContentAlignmentProperty.ToLiteral(HorizontalAlignment.Right)]
        ),
        new(
            "justify-items-stretch",
            [ContentControl.HorizontalContentAlignmentProperty.ToLiteral(HorizontalAlignment.Stretch)]
        ),
        new("items-start", [ContentControl.VerticalContentAlignmentProperty.ToLiteral(VerticalAlignment.Top)]),
        new(
            "items-center",
            [ContentControl.VerticalContentAlignmentProperty.ToLiteral(VerticalAlignment.Center)]
        ),
        new("items-end", [ContentControl.VerticalContentAlignmentProperty.ToLiteral(VerticalAlignment.Bottom)]),
        new(
            "items-stretch",
            [ContentControl.VerticalContentAlignmentProperty.ToLiteral(VerticalAlignment.Stretch)]
        ),
    ];
}
