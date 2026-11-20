using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.Composites;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>A control owning its own <see cref="Thickness"/> property, so a third-party composite host can target a real property no shipped host already writes.</summary>
public sealed class CompositeProbeControl : Control
{
    /// <summary>A real <see cref="Thickness"/>-valued styled property, composed by <see cref="FrameSides"/> below.</summary>
    public static readonly StyledProperty<Thickness> FrameThicknessProperty = AvaloniaProperty.Register<
        CompositeProbeControl,
        Thickness
    >(nameof(FrameThickness));

    /// <summary>See <see cref="FrameThicknessProperty"/>.</summary>
    public Thickness FrameThickness
    {
        get => GetValue(FrameThicknessProperty);
        set => SetValue(FrameThicknessProperty, value);
    }
}

/// <summary>
/// A third-party composite host written exactly the way a consumer would: four side attached properties, a
/// descriptor, and <see cref="CompositeProperty.Register"/> in the explicit static constructor — no access to the
/// engine's internal handler.
/// </summary>
internal static class FrameSides
{
    public static readonly AttachedProperty<double> FrameLeftProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxFrameLeft", typeof(FrameSides), double.NaN);

    public static readonly AttachedProperty<double> FrameTopProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxFrameTop", typeof(FrameSides), double.NaN);

    public static readonly AttachedProperty<double> FrameRightProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxFrameRight", typeof(FrameSides), double.NaN);

    public static readonly AttachedProperty<double> FrameBottomProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("EnxFrameBottom", typeof(FrameSides), double.NaN);

    public static CompositePropertyDescriptor Descriptor { get; } =
        CompositePropertyDescriptor.Create(
            CompositeProbeControl.FrameThicknessProperty,
            CompositeKind.Thickness,
            nameof(FrameSides),
            "Frame"
        );

    static FrameSides() =>
        CompositeProperty.Register(
            Descriptor,
            FrameLeftProperty,
            FrameTopProperty,
            FrameRightProperty,
            FrameBottomProperty
        );
}

/// <summary>
/// The public registration entry point: a third-party host can register its own recomposition without the
/// internal <c>CompositePropertyHandler</c>, and a descriptor whose kind doesn't match its real property is
/// rejected up front instead of failing when a side changes.
/// </summary>
public class CompositePropertyRegistrationTests
{
    [AvaloniaFact]
    public void ThirdPartyHost_RecomposesItsRealPropertyFromTheSides()
    {
        _ = FrameSides.Descriptor; // runs the host's static constructor, like any first side read would

        var style = new Style(selector => selector.Is<Control>().Class("probe-frame"));
        style.Setters.Add(new Setter(FrameSides.FrameLeftProperty, 1d));
        style.Setters.Add(new Setter(FrameSides.FrameTopProperty, 2d));
        style.Setters.Add(new Setter(FrameSides.FrameRightProperty, 3d));
        style.Setters.Add(new Setter(FrameSides.FrameBottomProperty, 4d));

        var control = new CompositeProbeControl { Classes = { "probe-frame" } };
        var window = new Window { Content = control, Styles = { style } };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(1, 2, 3, 4), control.FrameThickness);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Unset sides compose as <c>0</c>, exactly like the shipped hosts.</summary>
    [AvaloniaFact]
    public void ThirdPartyHost_ComposesUnsetSidesAsZero()
    {
        _ = FrameSides.Descriptor;

        var style = new Style(selector => selector.Is<Control>().Class("probe-partial"));
        style.Setters.Add(new Setter(FrameSides.FrameLeftProperty, 5d));
        style.Setters.Add(new Setter(FrameSides.FrameBottomProperty, 6d));

        var control = new CompositeProbeControl { Classes = { "probe-partial" } };
        var window = new Window { Content = control, Styles = { style } };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(5, 0, 0, 6), control.FrameThickness);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void DescriptorKindMismatch_Throws()
    {
        var descriptor = CompositePropertyDescriptor.Create(
            CompositeProbeControl.FrameThicknessProperty,
            CompositeKind.CornerRadius,
            "MismatchedCorners",
            "Mismatched"
        );

        var exception = Assert.Throws<ArgumentException>(() =>
            CompositeProperty.Register(
                descriptor,
                FrameSides.FrameLeftProperty,
                FrameSides.FrameTopProperty,
                FrameSides.FrameRightProperty,
                FrameSides.FrameBottomProperty
            )
        );

        Assert.Contains("StyledProperty<CornerRadius>", exception.Message);
    }
}
