using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>
/// Static rules setting the <c>Orientation</c> property of the panels/controls that expose it
/// (<see cref="StackPanel"/>, <see cref="WrapPanel"/>, <see cref="ProgressBar"/>, <see cref="ScrollBar"/>, <see cref="TickBar"/>).
/// </summary>
public static class FlexDirection
{
    public static readonly Rule.Static[] All =
    [
        new(
            "flex-row",
            [
                StackPanel.OrientationProperty.ToLiteral(Orientation.Horizontal),
                WrapPanel.OrientationProperty.ToLiteral(Orientation.Horizontal),
                ProgressBar.OrientationProperty.ToLiteral(Orientation.Horizontal),
                ScrollBar.OrientationProperty.ToLiteral(Orientation.Horizontal),
                TickBar.OrientationProperty.ToLiteral(Orientation.Horizontal),
            ]
        ),
        new(
            "flex-col",
            [
                StackPanel.OrientationProperty.ToLiteral(Orientation.Vertical),
                WrapPanel.OrientationProperty.ToLiteral(Orientation.Vertical),
                ProgressBar.OrientationProperty.ToLiteral(Orientation.Vertical),
                ScrollBar.OrientationProperty.ToLiteral(Orientation.Vertical),
                TickBar.OrientationProperty.ToLiteral(Orientation.Vertical),
            ]
        ),
    ];
}
