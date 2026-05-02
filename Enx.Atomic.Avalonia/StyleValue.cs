using Avalonia;
using Dunet;

namespace Enx.Atomic.Avalonia;

public abstract record StyleValue;

[Union]
public abstract partial record StyleValue<TValue> : StyleValue
{
    public required AvaloniaProperty<TValue> Property { get; init; }

    public partial record Literal(TValue Value);
    public partial record Resource(string Name);
}