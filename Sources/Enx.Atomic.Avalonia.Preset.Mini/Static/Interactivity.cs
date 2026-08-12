using Avalonia.Input;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules toggling <see cref="InputElement.IsEnabledProperty"/> and <see cref="InputElement.FocusableProperty"/>.</summary>
public static class Interactivity
{
    public static readonly Rule.Static[] All =
    [
        new("enabled", [InputElement.IsEnabledProperty.ToLiteral(true)]),
        new("disabled", [InputElement.IsEnabledProperty.ToLiteral(false)]),
        new("focusable", [InputElement.FocusableProperty.ToLiteral(true)]),
        new("not-focusable", [InputElement.FocusableProperty.ToLiteral(false)]),
    ];
}
