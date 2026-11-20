using Avalonia;
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Composites;

/// <summary>
/// The side host for <see cref="Border.CornerRadiusProperty"/>: <c>rounded-tl-*</c>, <c>rounded-tr-*</c>, ...
/// target these four attached properties and the class handler composes the real <c>CornerRadius</c>. See
/// <see cref="CompositePropertyDescriptor"/> for the host pattern.
///
/// <para>
/// Slot order is <c>TopLeft, TopRight, BottomRight, BottomLeft</c> — <see cref="CornerRadius"/>'s own
/// constructor order, not the left/top/right/bottom order the <see cref="Thickness"/>-valued hosts use. That is
/// why the fields here are declared corner by corner and why <see cref="CompositeKind"/> exists at all.
/// </para>
/// <para>
/// The explicit static constructor is load-bearing — see <see cref="MarginSides"/> for the
/// <c>beforefieldinit</c> reasoning, which applies identically to every host here.
/// </para>
/// </summary>
[EmittableGhostPropertyHost]
public static class CornerRadiusSides
{
    /// <summary>Top-left radius in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> CornerRadiusTopLeftProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxCornerRadiusTopLeft", typeof(CornerRadiusSides), double.NaN);

    /// <summary>Top-right radius in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> CornerRadiusTopRightProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxCornerRadiusTopRight", typeof(CornerRadiusSides), double.NaN);

    /// <summary>Bottom-right radius in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> CornerRadiusBottomRightProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxCornerRadiusBottomRight", typeof(CornerRadiusSides), double.NaN);

    /// <summary>Bottom-left radius in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> CornerRadiusBottomLeftProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxCornerRadiusBottomLeft", typeof(CornerRadiusSides), double.NaN);

    /// <summary>The machine-readable description of this host — what the code generator embeds a copy from.</summary>
    public static CompositePropertyDescriptor Descriptor { get; } =
        CompositePropertyDescriptor.Create(
            Border.CornerRadiusProperty,
            CompositeKind.CornerRadius,
            nameof(CornerRadiusSides),
            "CornerRadius"
        );

    static CornerRadiusSides() =>
        CompositeProperty.Register(
            Descriptor,
            CornerRadiusTopLeftProperty,
            CornerRadiusTopRightProperty,
            CornerRadiusBottomRightProperty,
            CornerRadiusBottomLeftProperty
        );
}
