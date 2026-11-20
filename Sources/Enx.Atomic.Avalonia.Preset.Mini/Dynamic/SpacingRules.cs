using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Enx.Atomic.Avalonia.Composites;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting margins from <see cref="ISpacingPart.Spacing"/> (<c>m-*</c>, <c>mx-*</c>, <c>my-*</c>,
/// <c>mt-*</c>, <c>mr-*</c>, <c>mb-*</c>, <c>ml-*</c>). A leading <c>-</c> negates the looked-up value.
///
/// <para>
/// Every direction — including the uniform one — writes only <see cref="MarginSides"/>' side attached properties
/// (<c>double</c>, <c>NaN</c> when unset); the host's class handler is what recomposes
/// <see cref="Layoutable.MarginProperty"/> from them. A uniform <c>m-*</c> writes all four sides rather than the
/// real property, so <c>m-4 mx-2</c> composes instead of replacing the whole <see cref="Thickness"/> (see
/// <c>ARCHITECTURE.md</c>, "Ghost properties"). Targeting <c>Control</c> explicitly is required twice over: the
/// attached property's <see cref="AvaloniaProperty.OwnerType"/> is the static <see cref="MarginSides"/> host (a
/// meaningless <c>Is&lt;MarginSides&gt;</c> selector), and the host registers its change handlers on
/// <see cref="Control"/> only — a bare <see cref="Layoutable"/> that isn't a <c>Control</c> would receive the
/// side value but never recompose it, so <c>Control</c> is the widest honest target here.
/// </para>
/// <para>
/// The uniform/axis/side directions are one regex rather than UnoCSS's three rule entries, so
/// <see cref="GetOrderRank"/> reports their relative order explicitly: <c>mx-2 ml-1</c> must let the side win
/// just like <c>m-4 mx-2</c> lets the axis win, whatever the source order.
/// </para>
/// </summary>
public partial class MarginRule<TTheme> : IOrderRankedDynamicRule<TTheme>
    where TTheme : class, ISpacingPart, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!context.Theme.Spacing.TryResolve(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return [];

        if (match.Groups["neg"].Success)
            value = -value;

        // The side properties are AttachedProperty<double>; the spacing scale is float, so convert explicitly —
        // a boxed float fails the setter's type check once Avalonia applies it.
        var pixels = (double)value;

        return match.Groups["side"].Value switch
        {
            "x" =>
            [
                MarginSides.MarginLeftProperty.ToLiteral(pixels, typeof(Control)),
                MarginSides.MarginRightProperty.ToLiteral(pixels, typeof(Control)),
            ],
            "y" =>
            [
                MarginSides.MarginTopProperty.ToLiteral(pixels, typeof(Control)),
                MarginSides.MarginBottomProperty.ToLiteral(pixels, typeof(Control)),
            ],
            "t" => [MarginSides.MarginTopProperty.ToLiteral(pixels, typeof(Control))],
            "r" => [MarginSides.MarginRightProperty.ToLiteral(pixels, typeof(Control))],
            "b" => [MarginSides.MarginBottomProperty.ToLiteral(pixels, typeof(Control))],
            "l" => [MarginSides.MarginLeftProperty.ToLiteral(pixels, typeof(Control))],
            _ =>
            [
                MarginSides.MarginLeftProperty.ToLiteral(pixels, typeof(Control)),
                MarginSides.MarginTopProperty.ToLiteral(pixels, typeof(Control)),
                MarginSides.MarginRightProperty.ToLiteral(pixels, typeof(Control)),
                MarginSides.MarginBottomProperty.ToLiteral(pixels, typeof(Control)),
            ],
        };
    }

    /// <summary>Uniform beats nothing (rank 0), an axis overrides the uniform sides it covers (1), a single side overrides its axis (2).</summary>
    public int GetOrderRank(Match match) =>
        match.Groups["side"].Value switch
        {
            "" => 0,
            "x" or "y" => 1,
            _ => 2,
        };

    [GeneratedRegex("^(?<neg>-)?m(?<side>[xytrbl])?-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}

/// <summary>
/// Dynamic rule setting padding from <see cref="ISpacingPart.Spacing"/> (<c>p-*</c>, <c>px-*</c>, <c>py-*</c>,
/// <c>pt-*</c>, <c>pr-*</c>, <c>pb-*</c>, <c>pl-*</c>). The uniform variant (<c>p-*</c>, no side) sets
/// <see cref="Decorator.PaddingProperty"/> directly; every other variant targets <see cref="SpecialProperties"/>
/// ghost properties instead of zeroing the sides it doesn't cover — see <see cref="GhostPropertyCombiner{TTheme}"/>.
/// </summary>
public partial class PaddingRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, ISpacingPart, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!context.Theme.Spacing.TryResolve(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return [];

        return match.Groups["side"].Value switch
        {
            "x" =>
            [
                SpecialProperties.PaddingLeftProperty.ToLiteral(value),
                SpecialProperties.PaddingRightProperty.ToLiteral(value),
            ],
            "y" =>
            [
                SpecialProperties.PaddingTopProperty.ToLiteral(value),
                SpecialProperties.PaddingBottomProperty.ToLiteral(value),
            ],
            "t" => [SpecialProperties.PaddingTopProperty.ToLiteral(value)],
            "r" => [SpecialProperties.PaddingRightProperty.ToLiteral(value)],
            "b" => [SpecialProperties.PaddingBottomProperty.ToLiteral(value)],
            "l" => [SpecialProperties.PaddingLeftProperty.ToLiteral(value)],
            _ => [Decorator.PaddingProperty.ToLiteral(new Thickness(value))],
        };
    }

    [GeneratedRegex("^p(?<side>[xytrbl])?-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}

/// <summary>
/// Dynamic rule setting the spacing between children from <see cref="ISpacingPart.Spacing"/> (<c>gap-*</c>,
/// <c>gap-x-*</c>, <c>gap-y-*</c>): <see cref="StackPanel.SpacingProperty"/> (uniform gap only, single axis) and
/// <see cref="Grid.ColumnSpacingProperty"/>/<see cref="Grid.RowSpacingProperty"/> with their <see cref="UniformGrid"/> equivalents.
/// </summary>
public partial class GapRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, ISpacingPart, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!context.Theme.Spacing.TryResolve(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return [];

        var v = (double)value;

        return match.Groups["axis"].Value switch
        {
            "x" => [Grid.ColumnSpacingProperty.ToLiteral(v), UniformGrid.ColumnSpacingProperty.ToLiteral(v)],
            "y" => [Grid.RowSpacingProperty.ToLiteral(v), UniformGrid.RowSpacingProperty.ToLiteral(v)],
            _ =>
            [
                StackPanel.SpacingProperty.ToLiteral(v),
                Grid.ColumnSpacingProperty.ToLiteral(v),
                Grid.RowSpacingProperty.ToLiteral(v),
                UniformGrid.ColumnSpacingProperty.ToLiteral(v),
                UniformGrid.RowSpacingProperty.ToLiteral(v),
            ],
        };
    }

    [GeneratedRegex("^gap-(?:(?<axis>[xy])-)?(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
