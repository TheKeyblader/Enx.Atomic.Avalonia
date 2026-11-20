using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Enx.Atomic.Avalonia.Composites;

/// <summary>
/// The side host for <see cref="Decorator.PaddingProperty"/>: <c>pl-*</c>, <c>pt-*</c>, ... target these four
/// attached properties and the class handler composes the real <c>Padding</c>. See
/// <see cref="CompositePropertyDescriptor"/> for the host pattern.
///
/// <para>
/// <c>Padding</c> is the <c>AddOwner</c> case of the four composites: <see cref="Decorator"/> registers it, but
/// <see cref="TemplatedControl"/>, <see cref="TextBlock"/> and <see cref="ContentPresenter"/> expose the very
/// same instance. The handler therefore checks <c>AvaloniaPropertyRegistry.Instance.IsRegistered</c> on the
/// concrete control instead of assuming the property exists (a <see cref="Canvas"/> has no <c>Padding</c>).
/// </para>
/// <para>
/// The explicit static constructor is load-bearing — see <see cref="MarginSides"/> for the
/// <c>beforefieldinit</c> reasoning, which applies identically to every host here.
/// </para>
/// </summary>
[EmittableGhostPropertyHost]
public static class PaddingSides
{
    /// <summary>Left padding in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> PaddingLeftProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxPaddingLeft", typeof(PaddingSides), double.NaN);

    /// <summary>Top padding in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> PaddingTopProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxPaddingTop", typeof(PaddingSides), double.NaN);

    /// <summary>Right padding in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> PaddingRightProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxPaddingRight", typeof(PaddingSides), double.NaN);

    /// <summary>Bottom padding in pixels; <see cref="double.NaN"/> means "this side is unset".</summary>
    public static readonly AttachedProperty<double> PaddingBottomProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxPaddingBottom", typeof(PaddingSides), double.NaN);

    /// <summary>The machine-readable description of this host — what the code generator embeds a copy from.</summary>
    public static CompositePropertyDescriptor Descriptor { get; } =
        CompositePropertyDescriptor.Create(
            Decorator.PaddingProperty,
            CompositeKind.Thickness,
            nameof(PaddingSides),
            "Padding"
        );

    static PaddingSides() =>
        CompositeProperty.Register(
            Descriptor,
            PaddingLeftProperty,
            PaddingTopProperty,
            PaddingRightProperty,
            PaddingBottomProperty
        );
}
