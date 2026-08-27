using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting margins from <see cref="ISpacingPart.Spacing"/> (<c>m-*</c>, <c>mx-*</c>, <c>my-*</c>,
/// <c>mt-*</c>, <c>mr-*</c>, <c>mb-*</c>, <c>ml-*</c>). A leading <c>-</c> negates the looked-up value. The
/// uniform variant (<c>m-*</c>, no side) sets <see cref="Layoutable.MarginProperty"/> directly; every other
/// variant targets <see cref="SpecialProperties"/> ghost properties instead of zeroing the sides it doesn't
/// cover — see <see cref="GhostPropertyCombiner{TTheme}"/>, which is what turns them into a real
/// <see cref="Layoutable.MarginProperty"/> value (combined with any sibling side found on the same source line,
/// or alone otherwise).
/// </summary>
public partial class MarginRule<TTheme> : IDynamicRule<TTheme>
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
                SpecialProperties.MarginLeftProperty.ToLiteral(value),
                SpecialProperties.MarginRightProperty.ToLiteral(value),
            ],
            "y" =>
            [
                SpecialProperties.MarginTopProperty.ToLiteral(value),
                SpecialProperties.MarginBottomProperty.ToLiteral(value),
            ],
            "t" => [SpecialProperties.MarginTopProperty.ToLiteral(value)],
            "r" => [SpecialProperties.MarginRightProperty.ToLiteral(value)],
            "b" => [SpecialProperties.MarginBottomProperty.ToLiteral(value)],
            "l" => [SpecialProperties.MarginLeftProperty.ToLiteral(value)],
            _ => [Layoutable.MarginProperty.ToLiteral(new Thickness(value))],
        };
    }

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
