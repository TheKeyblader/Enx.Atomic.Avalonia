using System.Reflection;
using Enx.Atomic.Avalonia.Composites;

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
    /// Returns a self-contained source copy of <paramref name="declaringType"/> if it's marked
    /// <see cref="EmittableGhostPropertyHostAttribute"/>, or <see langword="null"/> otherwise. A composite host
    /// (one exposing a <c>Descriptor</c>, see <see cref="CompositePropertyDescriptor"/>) is generated from that
    /// descriptor and additionally needs <see cref="CompositeHostSourceEmitter.HelperSource"/> emitted once;
    /// every other host still supplies a hand-written <c>EmbeddableSource</c> string.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The type is marked but supplies neither a valid <c>Descriptor</c> nor a public static string field named
    /// <c>EmbeddableSource</c>, or its <c>Descriptor</c> isn't a <see cref="CompositePropertyDescriptor"/>.
    /// </exception>
    public static EmittableGhostPropertyHostSource? TryGetHostSource(Type declaringType)
    {
        if (!declaringType.IsDefined(typeof(EmittableGhostPropertyHostAttribute)))
            return null;

        if (TryGetDescriptor(declaringType) is { } descriptor)
            return new EmittableGhostPropertyHostSource(
                CompositeHostSourceEmitter.EmitHost(descriptor),
                RequiresCompositeHelper: true
            );

        var field = declaringType.GetField("EmbeddableSource", BindingFlags.Public | BindingFlags.Static);
        if (field?.GetValue(null) is string source)
            return new EmittableGhostPropertyHostSource(source, RequiresCompositeHelper: false);

        throw new InvalidOperationException(
            $"'{declaringType}' is marked [EmittableGhostPropertyHost] but exposes neither a "
                + $"{nameof(CompositePropertyDescriptor)} 'Descriptor' nor a public static string field 'EmbeddableSource'."
        );
    }

    /// <summary>
    /// Reads a host's <c>Descriptor</c> (property or field, so either declaration style works). A host that
    /// declares one but with another type is a wiring mistake, not "fall back to EmbeddableSource".
    /// </summary>
    private static CompositePropertyDescriptor? TryGetDescriptor(Type declaringType)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.Static;

        var value =
            declaringType.GetProperty("Descriptor", Flags)?.GetValue(null)
            ?? declaringType.GetField("Descriptor", Flags)?.GetValue(null);

        if (value is null)
            return null;

        return value as CompositePropertyDescriptor
            ?? throw new InvalidOperationException(
                $"'{declaringType}.Descriptor' is a '{value.GetType()}' instead of a {nameof(CompositePropertyDescriptor)}."
            );
    }
}

/// <summary>
/// One embeddable host class's C# source, plus whether the generated file must also contain
/// <see cref="CompositeHostSourceEmitter.HelperSource"/> (true only for composite hosts).
/// </summary>
internal readonly record struct EmittableGhostPropertyHostSource(string Source, bool RequiresCompositeHelper);
