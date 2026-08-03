using Avalonia;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Convenience extensions for turning an <see cref="AvaloniaProperty{TValue}"/> into a <see cref="StyleValue"/>
/// that a <see cref="Rule"/> can return.
/// </summary>
public static class AvaloniaExtensions
{
    /// <summary>Creates a <see cref="StyleValue.Literal{TValue}"/> that sets <paramref name="property"/> to a fixed <paramref name="value"/>.</summary>
    public static StyleValue.Literal<TValue> ToLiteral<TValue>(
        this AvaloniaProperty<TValue> property,
        TValue value
    ) => new(property, value);

    /// <summary>Creates a <see cref="StyleValue.Resource"/> that sets <paramref name="property"/> via a <c>DynamicResource</c> lookup named <paramref name="name"/>.</summary>
    public static StyleValue.Resource ToResource<TValue>(
        this AvaloniaProperty<TValue> property,
        string name
    ) => new(property, name);
}
