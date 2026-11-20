using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
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


        return match.Groups["side"].Value switch
        {
            "x" =>
            [
                MarginSides.MarginLeftProperty.ToLiteral(value, typeof(Control)),
                MarginSides.MarginRightProperty.ToLiteral(value, typeof(Control)),
            ],
            "y" =>
            [
                MarginSides.MarginTopProperty.ToLiteral(value, typeof(Control)),
                MarginSides.MarginBottomProperty.ToLiteral(value, typeof(Control)),
            ],
            "t" => [MarginSides.MarginTopProperty.ToLiteral(value, typeof(Control))],
            "r" => [MarginSides.MarginRightProperty.ToLiteral(value, typeof(Control))],
            "b" => [MarginSides.MarginBottomProperty.ToLiteral(value, typeof(Control))],
            "l" => [MarginSides.MarginLeftProperty.ToLiteral(value, typeof(Control))],
            _ =>
            [
                MarginSides.MarginLeftProperty.ToLiteral(value, typeof(Control)),
                MarginSides.MarginTopProperty.ToLiteral(value, typeof(Control)),
                MarginSides.MarginRightProperty.ToLiteral(value, typeof(Control)),
                MarginSides.MarginBottomProperty.ToLiteral(value, typeof(Control)),
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
/// <c>pt-*</c>, <c>pr-*</c>, <c>pb-*</c>, <c>pl-*</c>).
///
/// <para>
/// Every direction — including the uniform one — writes only <see cref="PaddingSides"/>' side attached properties
/// (<c>double</c>, <c>NaN</c> when unset); the host's class handler recomposes
/// <see cref="Decorator.PaddingProperty"/> from them. A uniform <c>p-*</c> writes all four sides rather than the
/// real property, so <c>p-4 px-2</c> composes instead of replacing the whole <see cref="Thickness"/>.
/// </para>
/// <para>
/// Targeting <c>Control</c> explicitly is what fixes the old <c>Is&lt;Decorator&gt;</c> selector: Padding is
/// <see cref="Decorator.PaddingProperty"/> <c>AddOwner</c>'ed by <see cref="TemplatedControl"/>,
/// <see cref="TextBlock"/> and <see cref="ContentPresenter"/>, so a token scoped to <see cref="Decorator"/> alone
/// never reached a <see cref="Button"/> (a <see cref="TemplatedControl"/>). <see cref="Control"/> is the widest
/// honest target; the handler's <c>IsRegistered</c> guard is what keeps it a no-op on a control such as
/// <see cref="Canvas"/> that never registers Padding at all.
/// </para>
/// <para>
/// The uniform/axis/side directions are one regex rather than UnoCSS's three rule entries, so
/// <see cref="GetOrderRank"/> reports their relative order explicitly: <c>p-4 px-2</c> must let the axis win and
/// <c>px-2 pl-1</c> the side, whatever the source order.
/// </para>
/// </summary>
public partial class PaddingRule<TTheme> : IOrderRankedDynamicRule<TTheme>
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
                PaddingSides.PaddingLeftProperty.ToLiteral(value, typeof(Control)),
                PaddingSides.PaddingRightProperty.ToLiteral(value, typeof(Control)),
            ],
            "y" =>
            [
                PaddingSides.PaddingTopProperty.ToLiteral(value, typeof(Control)),
                PaddingSides.PaddingBottomProperty.ToLiteral(value, typeof(Control)),
            ],
            "t" => [PaddingSides.PaddingTopProperty.ToLiteral(value, typeof(Control))],
            "r" => [PaddingSides.PaddingRightProperty.ToLiteral(value, typeof(Control))],
            "b" => [PaddingSides.PaddingBottomProperty.ToLiteral(value, typeof(Control))],
            "l" => [PaddingSides.PaddingLeftProperty.ToLiteral(value, typeof(Control))],
            _ =>
            [
                PaddingSides.PaddingLeftProperty.ToLiteral(value, typeof(Control)),
                PaddingSides.PaddingTopProperty.ToLiteral(value, typeof(Control)),
                PaddingSides.PaddingRightProperty.ToLiteral(value, typeof(Control)),
                PaddingSides.PaddingBottomProperty.ToLiteral(value, typeof(Control)),
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

        return match.Groups["axis"].Value switch
        {
            "x" => [Grid.ColumnSpacingProperty.ToLiteral(value), UniformGrid.ColumnSpacingProperty.ToLiteral(value)],
            "y" => [Grid.RowSpacingProperty.ToLiteral(value), UniformGrid.RowSpacingProperty.ToLiteral(value)],
            _ =>
            [
                StackPanel.SpacingProperty.ToLiteral(value),
                Grid.ColumnSpacingProperty.ToLiteral(value),
                Grid.RowSpacingProperty.ToLiteral(value),
                UniformGrid.ColumnSpacingProperty.ToLiteral(value),
                UniformGrid.RowSpacingProperty.ToLiteral(value),
            ],
        };
    }

    [GeneratedRegex("^gap-(?:(?<axis>[xy])-)?(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
