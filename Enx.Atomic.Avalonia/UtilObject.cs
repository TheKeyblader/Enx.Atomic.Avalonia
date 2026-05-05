using Avalonia.Styling;

namespace Enx.Atomic.Avalonia;

internal record UtilObject
{
    public required Selector Selector { get; set; }
    public StyleQuery? ContainerQuery { get; set; }
    public ThemeVariant? ThemeVariant { get; set; }
    public StyleValue[] Entries { get; init; } = [];
    public int Sort { get; init; }
}
