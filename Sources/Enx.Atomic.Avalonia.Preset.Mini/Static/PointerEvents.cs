using Avalonia.Input;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules toggling <see cref="InputElement.IsHitTestVisibleProperty"/>.</summary>
public static class PointerEvents
{
    public static readonly Rule.Static[] All =
    [
        new("pointer-events-none", [InputElement.IsHitTestVisibleProperty.ToLiteral(false)]),
        new("pointer-events-auto", [InputElement.IsHitTestVisibleProperty.ToLiteral(true)]),
    ];
}
