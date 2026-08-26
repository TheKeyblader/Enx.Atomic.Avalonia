using System.Linq.Expressions;
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

    /// <summary>
    /// A style value that sets its property via a <c>DynamicResource</c> lookup under <see cref="Key"/>, so it
    /// tracks theme/resource changes at runtime instead of being fixed at resolution time like
    /// <see cref="Literal{TValue}"/>. <see cref="ThemeAccess"/> is the theme-scale expression a rule read to
    /// produce this value (e.g. <c>t =&gt; t.Colors[value]</c>) — kept as data (an <see cref="Expression"/>
    /// tree), not compiled+decompiled — so the codegen pipeline can later compile and invoke it once against a
    /// real <c>TTheme</c> instance to populate the actual resource dictionary this key resolves against (see
    /// <c>ResourceDictionaryEmitter</c> in <c>Enx.Atomic.Avalonia.CodeGen</c>), while <see cref="StyleEmitter"/>
    /// itself only ever needs <see cref="Key"/> to emit the <c>Setter</c>'s <c>DynamicResourceExtension</c>.
    /// <see cref="Key"/> is always derived from <see cref="ThemeAccess"/> (<see cref="ThemeResourceKey.From"/>),
    /// never passed in — that's what guarantees two rules reading the same theme entry always agree on the
    /// same key, and two rules reading different entries can never collide on one.
    /// </summary>
    public record Resource : StyleValue
    {
        /// <inheritdoc/>
        public override AvaloniaProperty UntypedProperty { get; }

        /// <inheritdoc/>
        public override object? UntypedValue { get; }

        /// <inheritdoc/>
        public override Type TargetType { get; }

        /// <summary>The resource dictionary key this value is looked up under, derived from <see cref="ThemeAccess"/> — see <see cref="ThemeResourceKey.From"/>.</summary>
        public string Key { get; }

        /// <summary>
        /// The theme-scale access this resource's value came from, e.g. <c>Expression&lt;Func&lt;TTheme,object&gt;&gt;</c>
        /// for <c>t =&gt; t.Colors[value]</c>. Untyped as a plain <see cref="LambdaExpression"/> so <see cref="StyleValue"/>
        /// doesn't need to be generic over <c>TTheme</c> — same rationale as <see cref="UntypedProperty"/>/<see cref="UntypedValue"/>.
        /// </summary>
        public LambdaExpression ThemeAccess { get; }

        /// <summary>Creates a resource-backed style value for <paramref name="property"/>, looked up under a key derived from <paramref name="themeAccess"/>, targeting <paramref name="targetType"/> or, if omitted, the default from <see cref="StyleValue.TargetType"/>.</summary>
        public Resource(AvaloniaProperty property, LambdaExpression themeAccess, Type? targetType = null)
        {
            UntypedProperty = property;
            ThemeAccess = themeAccess;
            Key = ThemeResourceKey.From(themeAccess);
            UntypedValue = new DynamicResourceExtension(Key);
            TargetType = targetType ?? DefaultTargetType(property);
        }
    }
}
