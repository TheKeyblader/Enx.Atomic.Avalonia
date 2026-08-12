using Avalonia;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules setting <see cref="Visual.FlowDirectionProperty"/>.</summary>
public static class Direction
{
    public static readonly Rule.Static[] All =
    [
        new("ltr", [Visual.FlowDirectionProperty.ToLiteral(FlowDirection.LeftToRight)]),
        new("rtl", [Visual.FlowDirectionProperty.ToLiteral(FlowDirection.RightToLeft)]),
    ];
}
