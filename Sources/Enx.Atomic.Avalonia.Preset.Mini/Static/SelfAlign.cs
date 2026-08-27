using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>
/// Static rules setting <see cref="Layoutable.HorizontalAlignmentProperty"/> (<c>justify-self-*</c>) and
/// <see cref="Layoutable.VerticalAlignmentProperty"/> (<c>self-*</c>) — how an element positions itself within
/// the space its parent allocates to it.
/// </summary>
public static class SelfAlign
{
    public static readonly Rule.Static[] All =
    [
        new("justify-self-start", [Layoutable.HorizontalAlignmentProperty.ToLiteral(HorizontalAlignment.Left)]),
        new("justify-self-center", [Layoutable.HorizontalAlignmentProperty.ToLiteral(HorizontalAlignment.Center)]),
        new("justify-self-end", [Layoutable.HorizontalAlignmentProperty.ToLiteral(HorizontalAlignment.Right)]),
        new("justify-self-stretch", [Layoutable.HorizontalAlignmentProperty.ToLiteral(HorizontalAlignment.Stretch)]),
        new("self-start", [Layoutable.VerticalAlignmentProperty.ToLiteral(VerticalAlignment.Top)]),
        new("self-center", [Layoutable.VerticalAlignmentProperty.ToLiteral(VerticalAlignment.Center)]),
        new("self-end", [Layoutable.VerticalAlignmentProperty.ToLiteral(VerticalAlignment.Bottom)]),
        new("self-stretch", [Layoutable.VerticalAlignmentProperty.ToLiteral(VerticalAlignment.Stretch)]),
    ];
}
