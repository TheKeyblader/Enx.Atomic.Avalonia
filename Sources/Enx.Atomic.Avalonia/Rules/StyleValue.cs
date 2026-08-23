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

    /// <summary>
    /// The type the generated selector should match against (<c>.Is(TargetType)</c>). Defaults to
    /// <see cref="UntypedProperty"/>'s <see cref="AvaloniaProperty.OwnerType"/>, but that default is only
    /// correct when the property has a single owner. <see cref="AvaloniaProperty{TValue}.AddOwner{TOwner}"/>
    /// lets several unrelated types share the exact same property instance (e.g. <c>Border</c> and
    /// <c>TemplatedControl</c> both exposing the same <c>BackgroundProperty</c>) — <c>OwnerType</c> always
    /// reports the type that originally registered it, never the type a rule accessed it through. A rule that
    /// needs to target one of those other owners (see <c>BackgroundColorRule</c>) must set this explicitly.
    /// </summary>
    public abstract Type TargetType { get; }

    /// <summary>A style value that sets its property to a fixed, compile-time-known value.</summary>
    public record Literal<TValue> : StyleValue
    {
        /// <summary>The property being set.</summary>
        public AvaloniaProperty<TValue> Property { get; }

        /// <summary>The value assigned to <see cref="Property"/>.</summary>
        public TValue Value { get; }

        /// <inheritdoc/>
        public override AvaloniaProperty UntypedProperty => Property;

        /// <inheritdoc/>
        public override object? UntypedValue => Value;

        /// <inheritdoc/>
        public override Type TargetType { get; }

        /// <summary>Creates a literal style value, targeting <paramref name="targetType"/> or, if omitted, <paramref name="property"/>'s own <see cref="AvaloniaProperty.OwnerType"/>.</summary>
        public Literal(AvaloniaProperty<TValue> property, TValue value, Type? targetType = null)
        {
            Property = property;
            Value = value;
            TargetType = targetType ?? property.OwnerType;
        }
    }

    /// <summary>A style value that sets its property via a <c>DynamicResource</c> lookup, so it tracks theme/resource changes at runtime.</summary>
    public record Resource : StyleValue
    {
        /// <inheritdoc/>
        public override AvaloniaProperty UntypedProperty { get; }

        /// <inheritdoc/>
        public override object? UntypedValue { get; }

        /// <inheritdoc/>
        public override Type TargetType { get; }

        /// <summary>Creates a resource-backed style value for <paramref name="property"/>, looked up under resource key <paramref name="name"/>, targeting <paramref name="targetType"/> or, if omitted, <paramref name="property"/>'s own <see cref="AvaloniaProperty.OwnerType"/>.</summary>
        public Resource(AvaloniaProperty property, string name, Type? targetType = null)
        {
            UntypedProperty = property;
            UntypedValue = new DynamicResourceExtension(name);
            TargetType = targetType ?? property.OwnerType;
        }
    }
}
