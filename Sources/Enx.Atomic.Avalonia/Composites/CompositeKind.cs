using Avalonia;

namespace Enx.Atomic.Avalonia.Composites;

/// <summary>
/// The shape of a composite Avalonia property that a <see cref="CompositePropertyDescriptor"/> decomposes into
/// four <see cref="double"/> sides. It decides two things no host may get wrong independently: the slot order
/// of the four side properties, and the struct constructor used to reassemble them — <see cref="Thickness"/>'s
/// <c>(left, top, right, bottom)</c> is a flat box, while <see cref="CornerRadius"/>'s
/// <c>(topLeft, topRight, bottomRight, bottomLeft)</c> walks the corners, so the two are not interchangeable
/// even though both carry four doubles.
/// </summary>
public enum CompositeKind
{
    /// <summary>A <see cref="Avalonia.Thickness"/>-valued property, slots ordered Left, Top, Right, Bottom.</summary>
    Thickness,

    /// <summary>A <see cref="Avalonia.CornerRadius"/>-valued property, slots ordered TopLeft, TopRight, BottomRight, BottomLeft.</summary>
    CornerRadius,
}
