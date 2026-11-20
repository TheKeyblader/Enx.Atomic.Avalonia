using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Composites;

/// <summary>
/// The side host for <see cref="Layoutable.MarginProperty"/>: <c>ml-*</c>, <c>mt-*</c>, ... target these four
/// attached properties instead of each writing the whole <see cref="Thickness"/>, and the class handler
/// registered here composes them into the real <c>Margin</c>. See
/// <see cref="CompositePropertyDescriptor"/> for the host pattern and <see cref="MarginSides"/>'s siblings for
/// the other composites.
///
/// <para>
/// Public because it is the runtime half of the feature (rules build <see cref="Avalonia.Styling.Setter"/>s
/// against these fields) and because it is the reference shape a user-defined host follows — the generated
/// copy embedded by <c>Enx.Atomic.Avalonia.CodeGen</c> is a <c>file</c>-local class with the same name, fields
/// and behavior.
/// </para>
/// <para>
/// The explicit static constructor is load-bearing: it makes the type <em>not</em> <c>beforefieldinit</c>, so
/// the runtime runs it before the first static field read. The only way to reach one of these properties is
/// such a read (building a <c>StyleValue</c>/<c>Setter</c>, or asking for <see cref="Descriptor"/>), which
/// means the class handlers are always registered before any side value can change.
/// </para>
/// </summary>
[EmittableGhostPropertyHost]
public static class MarginSides
{
    /// <summary>Left margin in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> MarginLeftProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxMarginLeft", typeof(MarginSides), double.NaN);

    /// <summary>Top margin in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> MarginTopProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxMarginTop", typeof(MarginSides), double.NaN);

    /// <summary>Right margin in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> MarginRightProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxMarginRight", typeof(MarginSides), double.NaN);

    /// <summary>Bottom margin in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> MarginBottomProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxMarginBottom", typeof(MarginSides), double.NaN);

    /// <summary>The machine-readable description of this host — what the code generator embeds a copy from.</summary>
    public static CompositePropertyDescriptor Descriptor { get; } =
        CompositePropertyDescriptor.Create(
            Layoutable.MarginProperty,
            CompositeKind.Thickness,
            nameof(MarginSides),
            "Margin"
        );

    static MarginSides() =>
        CompositeProperty.Register(
            Descriptor,
            MarginLeftProperty,
            MarginTopProperty,
            MarginRightProperty,
            MarginBottomProperty
        );
}
