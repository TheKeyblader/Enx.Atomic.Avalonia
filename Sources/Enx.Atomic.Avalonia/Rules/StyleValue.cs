using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// A single Avalonia property/value pair produced by a <see cref="Rule"/>, later turned into a
/// <see cref="Avalonia.Styling.Setter"/>. Values are exposed untyped so heterogeneous style values from a
/// rule can be grouped and processed together; see <see cref="AvaloniaExtensions.ToLiteral{TValue}"/> and
/// <see cref="AvaloniaExtensions.ToResource{TValue}"/> for the typed factory helpers.
/// </summary>
public abstract record StyleValue
{
    /// <summary>The Avalonia property being set.</summary>
    public abstract AvaloniaProperty UntypedProperty { get; }

    /// <summary>The value assigned to <see cref="UntypedProperty"/>.</summary>
    public abstract object? UntypedValue { get; }

    /// <summary>A style value that sets its property to a fixed, compile-time-known value.</summary>
    public record Literal<TValue>(AvaloniaProperty<TValue> Property, TValue Value) : StyleValue
    {
        /// <inheritdoc/>
        public override AvaloniaProperty UntypedProperty => Property;

        /// <inheritdoc/>
        public override object? UntypedValue => Value;
    }

    /// <summary>A style value that sets its property via a <c>DynamicResource</c> lookup, so it tracks theme/resource changes at runtime.</summary>
    public record Resource : StyleValue
    {
        /// <inheritdoc/>
        public override AvaloniaProperty UntypedProperty { get; }

        /// <inheritdoc/>
        public override object? UntypedValue { get; }

        /// <summary>Creates a resource-backed style value for <paramref name="property"/>, looked up under resource key <paramref name="name"/>.</summary>
        public Resource(AvaloniaProperty property, string name)
        {
            UntypedProperty = property;
            UntypedValue = new DynamicResourceExtension(name);
        }
    }
}
