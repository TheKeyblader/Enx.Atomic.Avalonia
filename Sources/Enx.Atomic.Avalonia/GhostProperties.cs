using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Hardcoded registry mapping every <see cref="SpecialProperties"/> ghost property to the real composite
/// property it contributes to, which of that composite's 4 slots it fills, and how to assemble a full
/// 4-slot group into the real value once <see cref="GhostPropertyCombiner{TTheme}"/> has collected them.
/// Not user-extensible yet — see <c>ARCHITECTURE.md</c>.
/// </summary>
public static class GhostProperties
{
    /// <param name="Real">The real composite property this ghost contributes a slot to.</param>
    /// <param name="Slot">Index (0-3) of the slot this ghost fills within that composite's value.</param>
    /// <param name="Build">Assembles a full 4-slot array into the real property's <see cref="StyleValue"/>. Identical for every ghost targeting the same <see cref="Real"/>, so any one of them can be used once a group is collected.</param>
    public sealed record Entry(AvaloniaProperty Real, int Slot, Func<float[], StyleValue> Build);

    public static readonly IReadOnlyDictionary<AvaloniaProperty, Entry> Map = new Dictionary<AvaloniaProperty, Entry>
    {
        // Margin: Thickness slot order is Left, Top, Right, Bottom.
        [SpecialProperties.MarginLeftProperty] = new(Layoutable.MarginProperty, 0, ThicknessSetter(Layoutable.MarginProperty)),
        [SpecialProperties.MarginTopProperty] = new(Layoutable.MarginProperty, 1, ThicknessSetter(Layoutable.MarginProperty)),
        [SpecialProperties.MarginRightProperty] = new(Layoutable.MarginProperty, 2, ThicknessSetter(Layoutable.MarginProperty)),
        [SpecialProperties.MarginBottomProperty] = new(Layoutable.MarginProperty, 3, ThicknessSetter(Layoutable.MarginProperty)),

        // Padding: same Thickness slot order.
        [SpecialProperties.PaddingLeftProperty] = new(Decorator.PaddingProperty, 0, ThicknessSetter(Decorator.PaddingProperty)),
        [SpecialProperties.PaddingTopProperty] = new(Decorator.PaddingProperty, 1, ThicknessSetter(Decorator.PaddingProperty)),
        [SpecialProperties.PaddingRightProperty] = new(Decorator.PaddingProperty, 2, ThicknessSetter(Decorator.PaddingProperty)),
        [SpecialProperties.PaddingBottomProperty] = new(Decorator.PaddingProperty, 3, ThicknessSetter(Decorator.PaddingProperty)),

        // Border width: same Thickness slot order. Provisioned for a future per-side border-width rule.
        [SpecialProperties.BorderThicknessLeftProperty] = new(Border.BorderThicknessProperty, 0, ThicknessSetter(Border.BorderThicknessProperty)),
        [SpecialProperties.BorderThicknessTopProperty] = new(Border.BorderThicknessProperty, 1, ThicknessSetter(Border.BorderThicknessProperty)),
        [SpecialProperties.BorderThicknessRightProperty] = new(Border.BorderThicknessProperty, 2, ThicknessSetter(Border.BorderThicknessProperty)),
        [SpecialProperties.BorderThicknessBottomProperty] = new(Border.BorderThicknessProperty, 3, ThicknessSetter(Border.BorderThicknessProperty)),

        // Corner radius: CornerRadius slot order is TopLeft, TopRight, BottomRight, BottomLeft.
        [SpecialProperties.CornerRadiusTopLeftProperty] = new(Border.CornerRadiusProperty, 0, CornerRadiusSetter),
        [SpecialProperties.CornerRadiusTopRightProperty] = new(Border.CornerRadiusProperty, 1, CornerRadiusSetter),
        [SpecialProperties.CornerRadiusBottomRightProperty] = new(Border.CornerRadiusProperty, 2, CornerRadiusSetter),
        [SpecialProperties.CornerRadiusBottomLeftProperty] = new(Border.CornerRadiusProperty, 3, CornerRadiusSetter),
    };

    private static Func<float[], StyleValue> ThicknessSetter(AvaloniaProperty<Thickness> property) =>
        slots => property.ToLiteral(new Thickness(slots[0], slots[1], slots[2], slots[3]));

    private static StyleValue CornerRadiusSetter(float[] slots) =>
        Border.CornerRadiusProperty.ToLiteral(new CornerRadius(slots[0], slots[1], slots[2], slots[3]));
}
