using System.Reflection;
using Avalonia;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// Resolves an <see cref="AvaloniaProperty"/> instance back to the C# expression that declares it (e.g.
/// <c>"Button.IsPressedProperty"</c>), by finding the public static field on its owner type whose value is
/// that exact instance — the same technique <c>SelectorExpression.PropertyEquals</c> uses to rebuild a
/// <see cref="System.Linq.Expressions.MemberExpression"/>, just producing text instead.
/// </summary>
public static class AvaloniaPropertyNaming
{
    private static readonly Dictionary<AvaloniaProperty, string> Cache = [];

    /// <summary>The C# expression referring to <paramref name="property"/>'s declaring static field.</summary>
    /// <exception cref="InvalidOperationException">No public static field on <see cref="AvaloniaProperty.OwnerType"/> holds this exact instance.</exception>
    public static string GetExpression(AvaloniaProperty property)
    {
        if (Cache.TryGetValue(property, out var expression))
            return expression;

        foreach (
            var field in property.OwnerType.GetFields(BindingFlags.Public | BindingFlags.Static)
        )
        {
            if (!ReferenceEquals(field.GetValue(null), property))
                continue;

            return Cache[property] = $"{CSharpTypeNaming.GetName(property.OwnerType)}.{field.Name}";
        }

        throw new InvalidOperationException(
            $"No public static field on '{property.OwnerType}' holds the AvaloniaProperty '{property.Name}'."
        );
    }
}
