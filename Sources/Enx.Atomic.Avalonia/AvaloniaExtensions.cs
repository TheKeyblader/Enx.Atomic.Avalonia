using System.Linq.Expressions;
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
    /// <c>DynamicResource</c> lookup, whose value is derived from <paramref name="theme"/> (e.g.
    /// <c>t =&gt; t.Colors[value]</c>) — see <see cref="StyleValue.Resource.ThemeAccess"/>. The resource key
    /// itself is derived automatically from <paramref name="theme"/>, not passed in — see
    /// <see cref="ThemeResourceKey.From"/>. See <paramref name="targetType"/> on <see cref="ToLiteral{TValue}"/>
    /// for when to pass it explicitly.
    /// </summary>
    public static StyleValue.Resource ToResource<TValue, TTheme>(
        this AvaloniaProperty<TValue> property,
        Expression<Func<TTheme, object>> theme,
        Type? targetType = null
    ) => new(property, theme, targetType);
}
