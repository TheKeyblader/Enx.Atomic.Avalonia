using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules setting <see cref="Image.StretchProperty"/> (and its equivalents on <see cref="Shape"/>/<see cref="Viewbox"/>).</summary>
public static class ObjectFit
{
    public static readonly Rule.Static[] All =
    [
        new(
            "object-fill",
            [
                Image.StretchProperty.ToLiteral(Stretch.Fill),
                Shape.StretchProperty.ToLiteral(Stretch.Fill),
                Viewbox.StretchProperty.ToLiteral(Stretch.Fill),
            ]
        ),
        new(
            "object-contain",
            [
                Image.StretchProperty.ToLiteral(Stretch.Uniform),
                Shape.StretchProperty.ToLiteral(Stretch.Uniform),
                Viewbox.StretchProperty.ToLiteral(Stretch.Uniform),
            ]
        ),
        new(
            "object-cover",
            [
                Image.StretchProperty.ToLiteral(Stretch.UniformToFill),
                Shape.StretchProperty.ToLiteral(Stretch.UniformToFill),
                Viewbox.StretchProperty.ToLiteral(Stretch.UniformToFill),
            ]
        ),
        new(
            "object-none",
            [
                Image.StretchProperty.ToLiteral(Stretch.None),
                Shape.StretchProperty.ToLiteral(Stretch.None),
                Viewbox.StretchProperty.ToLiteral(Stretch.None),
            ]
        ),
    ];
}
