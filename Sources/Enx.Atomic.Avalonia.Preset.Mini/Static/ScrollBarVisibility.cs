using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>
/// Static rules setting <see cref="ScrollViewer.HorizontalScrollBarVisibilityProperty"/> (<c>scroll-x-*</c>) and
/// <see cref="ScrollViewer.VerticalScrollBarVisibilityProperty"/> (<c>scroll-y-*</c>).
/// </summary>
public static class ScrollBarVisibilityRules
{
    public static readonly Rule.Static[] All =
    [
        new(
            "scroll-x-auto",
            [ScrollViewer.HorizontalScrollBarVisibilityProperty.ToLiteral(ScrollBarVisibility.Auto)]
        ),
        new(
            "scroll-x-hidden",
            [ScrollViewer.HorizontalScrollBarVisibilityProperty.ToLiteral(ScrollBarVisibility.Hidden)]
        ),
        new(
            "scroll-x-visible",
            [ScrollViewer.HorizontalScrollBarVisibilityProperty.ToLiteral(ScrollBarVisibility.Visible)]
        ),
        new(
            "scroll-x-disabled",
            [ScrollViewer.HorizontalScrollBarVisibilityProperty.ToLiteral(ScrollBarVisibility.Disabled)]
        ),
        new(
            "scroll-y-auto",
            [ScrollViewer.VerticalScrollBarVisibilityProperty.ToLiteral(ScrollBarVisibility.Auto)]
        ),
        new(
            "scroll-y-hidden",
            [ScrollViewer.VerticalScrollBarVisibilityProperty.ToLiteral(ScrollBarVisibility.Hidden)]
        ),
        new(
            "scroll-y-visible",
            [ScrollViewer.VerticalScrollBarVisibilityProperty.ToLiteral(ScrollBarVisibility.Visible)]
        ),
        new(
            "scroll-y-disabled",
            [ScrollViewer.VerticalScrollBarVisibilityProperty.ToLiteral(ScrollBarVisibility.Disabled)]
        ),
    ];
}
