using Avalonia;

namespace Enx.Atomic.Avalonia;

public static class AvaloniaExtensions
{
    public static StyleValue<TValue>.Literal ToLiteral<TValue>(this AvaloniaProperty<TValue> property, TValue value)
        => new StyleValue<TValue>.Literal(value) { Property = property };

    public static StyleValue<TValue>.Resource ToResource<TValue>(this AvaloniaProperty<TValue> property, string name)
        => new StyleValue<TValue>.Resource(name) { Property = property };
}