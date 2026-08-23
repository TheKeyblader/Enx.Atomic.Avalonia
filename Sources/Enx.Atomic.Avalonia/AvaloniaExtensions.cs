using Avalonia;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Convenience extensions for turning an <see cref="AvaloniaProperty{TValue}"/> into a <see cref="StyleValue"/>
/// that a <see cref="Rule"/> can return.
/// </summary>
public static class AvaloniaExtensions
{
    /// <summary>
    /// Creates a <see cref="StyleValue.Literal{TValue}"/> that sets <paramref name="property"/> to a fixed
    /// <paramref name="value"/>. Pass <paramref name="targetType"/> when <paramref name="property"/> is
    /// shared across owners via <see cref="AvaloniaProperty{TValue}.AddOwner{TOwner}"/> and you need the
    /// generated selector to target an owner other than <paramref name="property"/>'s own
    /// <see cref="AvaloniaProperty.OwnerType"/> — see <see cref="StyleValue.TargetType"/>.
    /// </summary>
    public static StyleValue.Literal<TValue> ToLiteral<TValue>(
        this AvaloniaProperty<TValue> property,
        TValue value,
        Type? targetType = null
    ) => new(property, value, targetType);

    /// <summary>
    /// Creates a <see cref="StyleValue.Resource"/> that sets <paramref name="property"/> via a
    /// <c>DynamicResource</c> lookup named <paramref name="name"/>. See <paramref name="targetType"/> on
    /// <see cref="ToLiteral{TValue}"/> for when to pass it explicitly.
    /// </summary>
    public static StyleValue.Resource ToResource<TValue>(
        this AvaloniaProperty<TValue> property,
        string name,
        Type? targetType = null
    ) => new(property, name, targetType);
}
