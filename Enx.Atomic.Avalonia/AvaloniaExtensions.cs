using Avalonia;

namespace Enx.Atomic.Avalonia;

public static class AvaloniaExtensions
{
    public static StyleValue.Literal<TValue> ToLiteral<TValue>(
        this AvaloniaProperty<TValue> property,
        TValue value
    ) => new(property, value);

    public static StyleValue.Resource ToResource<TValue>(
        this AvaloniaProperty<TValue> property,
        string name
    ) => new(property, name);
}
