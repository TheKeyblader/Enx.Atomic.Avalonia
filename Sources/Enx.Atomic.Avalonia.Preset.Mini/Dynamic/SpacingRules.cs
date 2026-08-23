using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="Layoutable.MarginProperty"/> from <see cref="ISpacingPart.Spacing"/> (<c>m-*</c>,
/// <c>mx-*</c>, <c>my-*</c>, <c>mt-*</c>, <c>mr-*</c>, <c>mb-*</c>, <c>ml-*</c>). A leading <c>-</c> negates the
/// looked-up value. Setting a single side/axis zeroes the sides not covered, since <see cref="Thickness"/> is one
/// struct-valued property rather than four independent ones.
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

        var thickness = match.Groups["side"].Value switch
        {
            "x" => new Thickness(value, 0),
            "y" => new Thickness(0, value),
            "t" => new Thickness(0, value, 0, 0),
            "r" => new Thickness(0, 0, value, 0),
            "b" => new Thickness(0, 0, 0, value),
            "l" => new Thickness(value, 0, 0, 0),
            _ => new Thickness(value),
        };

        return [Layoutable.MarginProperty.ToLiteral(thickness)];
    }

    [GeneratedRegex("^(?<neg>-)?m(?<side>[xytrbl])?-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}

/// <summary>
/// Dynamic rule setting <see cref="Decorator.PaddingProperty"/> from <see cref="ISpacingPart.Spacing"/> (<c>p-*</c>,
/// <c>px-*</c>, <c>py-*</c>, <c>pt-*</c>, <c>pr-*</c>, <c>pb-*</c>, <c>pl-*</c>). Setting a single side/axis zeroes
/// the sides not covered, since <see cref="Thickness"/> is one struct-valued property rather than four independent ones.
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

        var thickness = match.Groups["side"].Value switch
        {
            "x" => new Thickness(value, 0),
            "y" => new Thickness(0, value),
            "t" => new Thickness(0, value, 0, 0),
            "r" => new Thickness(0, 0, value, 0),
            "b" => new Thickness(0, 0, 0, value),
            "l" => new Thickness(value, 0, 0, 0),
            _ => new Thickness(value),
        };

        return [Decorator.PaddingProperty.ToLiteral(thickness)];
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
            "x" =>
            [
                Grid.ColumnSpacingProperty.ToLiteral(v),
                UniformGrid.ColumnSpacingProperty.ToLiteral(v),
            ],
            "y" =>
            [
                Grid.RowSpacingProperty.ToLiteral(v),
                UniformGrid.RowSpacingProperty.ToLiteral(v),
            ],
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
