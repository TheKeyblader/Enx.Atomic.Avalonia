using System.Reflection;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// Discovers, purely via reflection, whether an <see cref="AvaloniaProperty"/>'s declaring type opts into
/// <see cref="EmittableGhostPropertyHostAttribute"/> — a preset assembly is never a compile-time dependency of
/// this project, only the attribute type itself is (declared in the core engine, which this project already
/// references), so a preset can define a new embeddable ghost property host without this project knowing
/// about it ahead of time.
/// </summary>
internal static class EmittableGhostPropertyEmitter
{
    /// <summary>
    /// Returns <paramref name="declaringType"/>'s <c>EmbeddableSource</c> if it's marked <see cref="EmittableGhostPropertyHostAttribute"/>, or <see langword="null"/> otherwise.
    /// </summary>
    /// <exception cref="InvalidOperationException">The type is marked but has no public static string field named <c>EmbeddableSource</c>.</exception>
    public static string? TryGetEmbeddableSource(Type declaringType)
    {
        if (!declaringType.IsDefined(typeof(EmittableGhostPropertyHostAttribute)))
            return null;

        var field = declaringType.GetField("EmbeddableSource", BindingFlags.Public | BindingFlags.Static);
        if (field?.GetValue(null) is string source)
            return source;

        throw new InvalidOperationException(
            $"'{declaringType}' is marked [EmittableGhostPropertyHost] but has no public static string field 'EmbeddableSource'."
        );
    }
}
