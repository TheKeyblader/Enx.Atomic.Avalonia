using System.Linq.Expressions;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia;

public record VariantHandlerContext
{
    public required SelectorExpression Selector { get; set; }
    public required StyleValue[] Entries { get; set; }
    public required StyleQueryExpression? ContainerQuery { get; set; }
    public ThemeVariant? ThemeVariant { get; set; }
    public int Sort { get; set; }
}
