using System.Reflection;

namespace Enx.Atomic.Avalonia.Internal;

/// <summary>
/// Looks up the public static Avalonia factory methods (<c>Selectors.Class</c>, <c>StyleQueries.Width</c>, ...)
/// the <c>Compact</c> expression nodes compile calls to. Centralised so a signature change in a future Avalonia
/// version fails with one consistent, actionable message naming the missing overload.
/// </summary>
internal static class ReflectedApi
{
    /// <summary>Returns the public static method <paramref name="type"/>.<paramref name="name"/> with exactly <paramref name="parameterTypes"/>.</summary>
    /// <exception cref="InvalidOperationException">No such overload exists.</exception>
    public static MethodInfo RequireStaticMethod(Type type, string name, params Type[] parameterTypes) =>
        type.GetMethod(name, BindingFlags.Static | BindingFlags.Public, parameterTypes)
        ?? throw new InvalidOperationException(
            $"Could not find {type.Name}.{name}({string.Join(", ", parameterTypes.Select(t => t.Name))}); "
                + "the referenced Avalonia version's public API no longer matches what Enx.Atomic.Avalonia expects."
        );
}
