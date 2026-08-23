namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>Dispatches a value to the first registered <see cref="IValueEmitter"/> that can handle its type.</summary>
public sealed class ValueEmitterRegistry(IEnumerable<IValueEmitter>? emitters = null)
{
    public static readonly IReadOnlyList<IValueEmitter> Default =
    [
        new PrimitiveValueEmitter(),
        new EnumValueEmitter(),
        new ThicknessValueEmitter(),
        new CornerRadiusValueEmitter(),
        new BrushValueEmitter(),
        new CursorValueEmitter(),
        new TextDecorationsValueEmitter(),
    ];

    private readonly IReadOnlyList<IValueEmitter> _emitters = [.. emitters ?? Default];

    /// <summary>Emits <paramref name="value"/> as C# text, adding any namespaces it needs to <paramref name="namespaces"/>. Returns <c>"null"</c> for a <see langword="null"/> value.</summary>
    /// <exception cref="NotSupportedException">No registered emitter handles <paramref name="value"/>'s type.</exception>
    public string Emit(object? value, ISet<string> namespaces)
    {
        if (value is null)
            return "null";

        var emitter =
            _emitters.FirstOrDefault(e => e.CanHandle(value.GetType()))
            ?? throw new NotSupportedException($"No value emitter registered for type '{value.GetType()}'.");

        foreach (var ns in emitter.GetNamespaces(value))
            namespaces.Add(ns);

        return emitter.Emit(value);
    }
}
