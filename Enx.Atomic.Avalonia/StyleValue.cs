using Avalonia;
using Dunet;

namespace Enx.Atomic.Avalonia;

public abstract record StyleValue
{
    public abstract AvaloniaProperty UntypedProperty { get; }
}

[Union]
public abstract partial record StyleValue<TValue> : StyleValue
{
    public override AvaloniaProperty UntypedProperty => Property;
    public required AvaloniaProperty<TValue> Property { get; init; }

    public partial record Literal(TValue Value);
    public partial record Resource(string Name);
}