using Avalonia.Styling;
using System.Linq.Expressions;

namespace Enx.Atomic.Avalonia;

public record VariantHandlerContext
{
    public required Expression Selector { get; set; }
    public required StyleValue[] Entries { get; set; }
    public required Expression ContainerQuery { get; set; }
    public ThemeVariant? ThemeVariant { get; set; }
    public int Sort { get; set; }
}
