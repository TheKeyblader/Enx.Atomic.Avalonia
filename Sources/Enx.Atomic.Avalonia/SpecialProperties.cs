using Avalonia;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Ghost properties: real, registered <see cref="AvaloniaProperty{TValue}"/> instances whose owner type
/// (this class) is never referenced by a consuming project — see the "Source transformers and ghost
/// properties" section of <c>ARCHITECTURE.md</c>. A dynamic rule can target one of these instead of building
/// a zeroed struct on the real composite property directly (e.g. <c>pl-4</c> targeting
/// <see cref="PaddingLeftProperty"/> instead of zeroing three sides of <c>Decorator.PaddingProperty</c>);
/// <see cref="GhostPropertyCombiner{TTheme}"/> recognizes them via <see cref="GhostProperties.Map"/> and
/// assembles the real composite value. Derives from <see cref="StyledElement"/> — not
/// <see cref="AvaloniaObject"/> — purely because <see cref="Avalonia.Styling.Selectors.Is{T}"/> requires it;
/// no instance of this type is ever created.
/// </summary>
public class SpecialProperties : StyledElement
{
    // Padding (Decorator.PaddingProperty)
    public static readonly AvaloniaProperty<float> PaddingLeftProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("PaddingLeft");

    public static readonly AvaloniaProperty<float> PaddingTopProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("PaddingTop");

    public static readonly AvaloniaProperty<float> PaddingRightProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("PaddingRight");

    public static readonly AvaloniaProperty<float> PaddingBottomProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("PaddingBottom");

    // Border width (Border.BorderThicknessProperty). Consumed by BorderWidthRule's per-side variants
    // (border-t-*, border-r-*, border-b-*, border-l-*).
    public static readonly AvaloniaProperty<float> BorderThicknessLeftProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("BorderThicknessLeft");

    public static readonly AvaloniaProperty<float> BorderThicknessTopProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("BorderThicknessTop");

    public static readonly AvaloniaProperty<float> BorderThicknessRightProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("BorderThicknessRight");

    public static readonly AvaloniaProperty<float> BorderThicknessBottomProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("BorderThicknessBottom");

    // Corner radius (Border.CornerRadiusProperty). Slot order follows CornerRadius's own 4-argument
    // constructor: TopLeft, TopRight, BottomRight, BottomLeft — not the Left/Top/Right/Bottom order the
    // Thickness-valued properties above use.
    public static readonly AvaloniaProperty<float> CornerRadiusTopLeftProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("CornerRadiusTopLeft");

    public static readonly AvaloniaProperty<float> CornerRadiusTopRightProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("CornerRadiusTopRight");

    public static readonly AvaloniaProperty<float> CornerRadiusBottomRightProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("CornerRadiusBottomRight");

    public static readonly AvaloniaProperty<float> CornerRadiusBottomLeftProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("CornerRadiusBottomLeft");
}
