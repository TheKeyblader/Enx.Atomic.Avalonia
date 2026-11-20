using Avalonia;
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Composites;

/// <summary>
/// The side host for <see cref="Border.BorderThicknessProperty"/>: <c>border-l-*</c>, <c>border-t-*</c>, ...
/// target these four attached properties and the class handler composes the real <c>BorderThickness</c>. See
/// <see cref="CompositePropertyDescriptor"/> for the host pattern.
///
/// <para>
/// The explicit static constructor is load-bearing — see <see cref="MarginSides"/> for the
/// <c>beforefieldinit</c> reasoning, which applies identically to every host here.
/// </para>
/// </summary>
[EmittableGhostPropertyHost]
public static class BorderThicknessSides
{
    /// <summary>Left border width in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> BorderThicknessLeftProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxBorderThicknessLeft", typeof(BorderThicknessSides), double.NaN);

    /// <summary>Top border width in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> BorderThicknessTopProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxBorderThicknessTop", typeof(BorderThicknessSides), double.NaN);

    /// <summary>Right border width in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> BorderThicknessRightProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxBorderThicknessRight", typeof(BorderThicknessSides), double.NaN);

    /// <summary>Bottom border width in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> BorderThicknessBottomProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxBorderThicknessBottom", typeof(BorderThicknessSides), double.NaN);

    /// <summary>The machine-readable description of this host — what the code generator embeds a copy from.</summary>
    public static CompositePropertyDescriptor Descriptor { get; } =
        CompositePropertyDescriptor.Create(
            Border.BorderThicknessProperty,
            CompositeKind.Thickness,
            nameof(BorderThicknessSides),
            "BorderThickness"
        );

    static BorderThicknessSides() =>
        CompositeProperty.Register(
            Descriptor,
            BorderThicknessLeftProperty,
            BorderThicknessTopProperty,
            BorderThicknessRightProperty,
            BorderThicknessBottomProperty
        );
}
