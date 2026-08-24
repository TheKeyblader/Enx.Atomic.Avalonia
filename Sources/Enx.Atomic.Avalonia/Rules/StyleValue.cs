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
    /// <see cref="UntypedProperty"/>'s <see cref="AvaloniaProperty.OwnerType"/> for a plain styled property,
    /// but that default is only correct when the property has a single owner.
    /// <see cref="AvaloniaProperty{TValue}.AddOwner{TOwner}"/> lets several unrelated types share the exact
    /// same property instance (e.g. <c>Border</c> and <c>TemplatedControl</c> both exposing the same
    /// <c>BackgroundProperty</c>) — <c>OwnerType</c> always reports the type that originally registered it,
    /// never the type a rule accessed it through. A rule that needs to target one of those other owners (see
    /// <c>BackgroundColorRule</c>) must set this explicitly.
    ///
    /// For an <see cref="AvaloniaProperty.IsAttached"/> property, <c>OwnerType</c> isn't even the right
    /// starting point — it names whatever type happened to declare it (e.g. <c>Grid</c> for
    /// <c>Grid.ColumnSpanProperty</c>), not the (usually much wider) set of types it can actually be set on
    /// (any <c>Control</c>, for that one) — a rule setting <c>col-span-*</c> on a grid *child* would otherwise
    /// end up with a selector matching <c>Grid</c> itself. The default for those is <see cref="StyledElement"/>
    /// instead, the widest type <c>Selectors.Is</c> accepts — narrow it explicitly if a rule knows the actual
    /// usage is narrower.
    /// </summary>
    public abstract Type TargetType { get; }

    /// <summary>The default <see cref="TargetType"/> for <paramref name="property"/> when a rule doesn't override it explicitly — see <see cref="TargetType"/>.</summary>
    private protected static Type DefaultTargetType(AvaloniaProperty property) =>
        property.IsAttached ? typeof(StyledElement) : property.OwnerType;

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

        /// <summary>Creates a literal style value, targeting <paramref name="targetType"/> or, if omitted, the default from <see cref="StyleValue.TargetType"/>.</summary>
        public Literal(AvaloniaProperty<TValue> property, TValue value, Type? targetType = null)
        {
            Property = property;
            Value = value;
            TargetType = targetType ?? DefaultTargetType(property);
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

        /// <summary>Creates a resource-backed style value for <paramref name="property"/>, looked up under resource key <paramref name="name"/>, targeting <paramref name="targetType"/> or, if omitted, the default from <see cref="StyleValue.TargetType"/>.</summary>
        public Resource(AvaloniaProperty property, string name, Type? targetType = null)
        {
            UntypedProperty = property;
            UntypedValue = new DynamicResourceExtension(name);
            TargetType = targetType ?? DefaultTargetType(property);
        }
    }
}
