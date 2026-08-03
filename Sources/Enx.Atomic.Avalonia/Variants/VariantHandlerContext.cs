using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia;

/// <summary>The in-progress state of a style as it flows through the composed <see cref="VariantHandlerBase"/> pipeline.</summary>
public record VariantHandlerContext
{
    /// <summary>The selector built up so far, as an expression tree the emitter can compile.</summary>
    public required SelectorExpression Selector { get; set; }

    /// <summary>The style values (property/value pairs) being applied.</summary>
    public required StyleValue[] Entries { get; set; }

    /// <summary>The container query built up so far, or <see langword="null"/> if the style is not container-scoped.</summary>
    public required StyleQueryExpression? ContainerQuery { get; set; }

    /// <summary>Relative ordering hint for the resulting style, used to break ties when multiple utils target the same selector.</summary>
    public int Sort { get; set; }
}
