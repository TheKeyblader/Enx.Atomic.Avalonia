using Avalonia;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules toggling <see cref="Visual.ClipToBoundsProperty"/>.</summary>
public static class Clipping
{
    public static readonly Rule.Static[] All =
    [
        new("overflow-hidden", [Visual.ClipToBoundsProperty.ToLiteral(true)]),
        new("overflow-visible", [Visual.ClipToBoundsProperty.ToLiteral(false)]),
    ];
}
