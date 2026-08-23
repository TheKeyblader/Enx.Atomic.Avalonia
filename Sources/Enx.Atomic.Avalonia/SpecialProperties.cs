using Avalonia;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Ghost properties: real, registered <see cref="AvaloniaProperty{TValue}"/> instances whose owner type
/// (this class) is never referenced by a consuming project — see the "Source transformers and ghost
/// properties" section of <c>ARCHITECTURE.md</c>. A dynamic rule can target one of these instead of building
/// a zeroed struct on the real composite property directly (e.g. <c>ml-4</c> targeting
/// <see cref="MarginLeftProperty"/> instead of zeroing three sides of <c>Layoutable.MarginProperty</c>);
/// <see cref="GhostPropertyCombiner{TTheme}"/> recognizes them via <see cref="GhostProperties.Map"/> and
/// assembles the real composite value. Derives from <see cref="StyledElement"/> — not
/// <see cref="AvaloniaObject"/> — purely because <see cref="Avalonia.Styling.Selectors.Is{T}"/> requires it;
/// no instance of this type is ever created.
/// </summary>
public class SpecialProperties : StyledElement
{
    // Margin (Layoutable.MarginProperty)
    public static readonly AvaloniaProperty<float> MarginLeftProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("MarginLeft");

    public static readonly AvaloniaProperty<float> MarginTopProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("MarginTop");

    public static readonly AvaloniaProperty<float> MarginRightProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("MarginRight");

    public static readonly AvaloniaProperty<float> MarginBottomProperty = AvaloniaProperty.Register<
        SpecialProperties,
        float
    >("MarginBottom");

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

    // Border width (Border.BorderThicknessProperty). Not consumed by any Preset.Mini rule yet — there is no
    // per-side border-width utility today (only the uniform border-*) — but provisioned for when one exists.
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
