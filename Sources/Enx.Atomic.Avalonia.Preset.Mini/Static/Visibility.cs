using Avalonia;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules toggling <see cref="Visual.IsVisibleProperty"/>.</summary>
public static class Visibility
{
    public static readonly Rule.Static[] All =
    [
        new("hidden", [Visual.IsVisibleProperty.ToLiteral(false)]),
        new("visible", [Visual.IsVisibleProperty.ToLiteral(true)]),
    ];
}
