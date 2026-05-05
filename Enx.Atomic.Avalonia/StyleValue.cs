using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Dunet;

namespace Enx.Atomic.Avalonia;

public abstract record StyleValue
{
    public abstract AvaloniaProperty UntypedProperty { get; }
    public abstract object? UntypedValue { get; }

    public record Literal<TValue>(AvaloniaProperty<TValue> Property, TValue Value) : StyleValue
    {
        public override AvaloniaProperty UntypedProperty => Property;
        public override object? UntypedValue => Value;
    }

    public record Resource : StyleValue
    {
        public override AvaloniaProperty UntypedProperty { get; }
        public override object? UntypedValue { get; }

        public Resource(AvaloniaProperty property, string name)
        {
            UntypedProperty = property;
            UntypedValue = new DynamicResourceExtension(name);
        }
    }
}
