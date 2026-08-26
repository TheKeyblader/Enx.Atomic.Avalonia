using System.Reflection;
using Avalonia;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// Resolves an <see cref="AvaloniaProperty"/> instance back to the C# expression that declares it (e.g.
/// <c>"Button.IsPressedProperty"</c>), by finding the public static field on its owner type whose value is
/// that exact instance — the same technique <c>SelectorExpression.PropertyEquals</c> uses to rebuild a
/// <see cref="System.Linq.Expressions.MemberExpression"/>, just producing text instead.
/// </summary>
internal static class AvaloniaPropertyNaming
{
    private static readonly Dictionary<(AvaloniaProperty Property, Type DeclaringType), string> FieldNameCache = [];

    /// <summary>
    /// The name of <paramref name="property"/>'s declaring static field, searched on <paramref name="declaringType"/>
    /// or, if omitted, <paramref name="property"/>'s own <see cref="AvaloniaProperty.OwnerType"/>. An explicit
    /// <paramref name="declaringType"/> is needed when <see cref="AvaloniaProperty.OwnerType"/> reports the
    /// type that originally registered the property (see <see cref="AvaloniaProperty{TValue}.AddOwner{TOwner}"/>)
    /// rather than the type it was actually reached through — <c>OwnerType</c> can even name a type whose
    /// declaring field isn't public. Also what <see cref="EmittableGhostPropertyEmitter"/> reuses when the
    /// field is being referenced through an embedded copy of its declaring type rather than the original.
    /// </summary>
    /// <exception cref="InvalidOperationException">No public static field on the resolved declaring type holds this exact instance.</exception>
    public static string GetFieldName(AvaloniaProperty property, Type? declaringType = null)
    {
        var owner = declaringType ?? property.OwnerType;
        var key = (property, owner);
        if (FieldNameCache.TryGetValue(key, out var name))
            return name;

        foreach (var field in owner.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (!ReferenceEquals(field.GetValue(null), property))
                continue;

            return FieldNameCache[key] = field.Name;
        }

        throw new InvalidOperationException(
            $"No public static field on '{owner}' holds the AvaloniaProperty '{property.Name}'."
        );
    }

    /// <summary>The C# expression referring to <paramref name="property"/>'s declaring static field — see <see cref="GetFieldName"/>.</summary>
    public static string GetExpression(AvaloniaProperty property, Type? declaringType = null)
    {
        var owner = declaringType ?? property.OwnerType;
        return $"{CSharpTypeNaming.GetName(owner)}.{GetFieldName(property, declaringType)}";
    }
}
