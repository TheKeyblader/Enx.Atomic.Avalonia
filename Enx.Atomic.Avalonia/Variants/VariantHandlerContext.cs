using Avalonia.Styling;

namespace Enx.Atomic.Avalonia;

public record VariantHandlerContext
{
    public required Selector Selector { get; set; }
    public required StyleQuery? ContainerQuery { get; set; }
    public required ThemeVariant? ThemeVariant { get; set; }
    public required StyleValue[] Entries { get; set; }
    public int Sort { get; set; }
}
