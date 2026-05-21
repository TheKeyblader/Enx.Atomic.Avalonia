using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Preset.Mini.Rules;

public static class LayoutableRules<TTheme>
    where TTheme : MiniTheme
{
    public static IRule[] Get() =>
        [
            // WidthProperty
            // HeightProperty
            // MinWidthProperty
            // MaxWidthProperty
            // MinHeightProperty
            // MaxHeightProperty
            new SpacingRule<TTheme>(),
            // MarginProperty
            new MarginRule<TTheme>(),
            // HorizontalAlignmentProperty
            new Rule.Static(
                "h-stretch",
                [Layoutable.HorizontalAlignmentProperty.ToLiteral(HorizontalAlignment.Stretch)]
            ),
            new Rule.Static(
                "h-left",
                [Layoutable.HorizontalAlignmentProperty.ToLiteral(HorizontalAlignment.Left)]
            ),
            new Rule.Static(
                "h-center",
                [Layoutable.HorizontalAlignmentProperty.ToLiteral(HorizontalAlignment.Center)]
            ),
            new Rule.Static(
                "h-right",
                [Layoutable.HorizontalAlignmentProperty.ToLiteral(HorizontalAlignment.Right)]
            ),
            // VerticalAlignmentProperty
            new Rule.Static(
                "v-stretch",
                [Layoutable.VerticalAlignmentProperty.ToLiteral(VerticalAlignment.Stretch)]
            ),
            new Rule.Static(
                "v-top",
                [Layoutable.VerticalAlignmentProperty.ToLiteral(VerticalAlignment.Top)]
            ),
            new Rule.Static(
                "v-center",
                [Layoutable.VerticalAlignmentProperty.ToLiteral(VerticalAlignment.Center)]
            ),
            new Rule.Static(
                "v-bottom",
                [Layoutable.VerticalAlignmentProperty.ToLiteral(VerticalAlignment.Bottom)]
            ),
            // UseLayoutRoundingProperty
            new Rule.Static("layout-round", [Layoutable.UseLayoutRoundingProperty.ToLiteral(true)]),
        ];
}

public partial record SpacingRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : MiniTheme
{
    public RuleMetadata Metadata { get; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!match.Groups[1].Success)
            return [];

        AvaloniaProperty<double> prop =
            match.Groups[1].Value == "h" ? Layoutable.HeightProperty : Layoutable.WidthProperty;

        if (match.Groups[0].Success)
        {
            prop =
                match.Groups[0].Value == "min"
                    ? (
                        prop == Layoutable.HeightProperty
                            ? Layoutable.MinHeightProperty
                            : Layoutable.MinWidthProperty
                    )
                    : (
                        prop == Layoutable.HeightProperty
                            ? Layoutable.MaxHeightProperty
                            : Layoutable.MaxWidthProperty
                    );
        }

        double? size = null;
        var valueSize = match.Groups[2].Value;
        if (context.Theme.Spacing.TryGetValue(valueSize, out var foundSize))
            size = context.Theme.RemToPxRatio * foundSize;

        if (double.TryParse(valueSize, out foundSize))
            size = context.Theme.RemToPxRatio * foundSize;

        if (!size.HasValue)
            return [];

        return [prop.ToLiteral(size.Value)];
    }

    [GeneratedRegex(@"^(min|max)?-?([wh])-(.+)$")]
    private static partial Regex CompiledRegex();
}

public partial record MarginRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : MiniTheme
{
    public RuleMetadata Metadata { get; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!match.Groups[1].Success)
            return [];

        double? size = null;
        var valueSize = match.Groups[2].Value;
        if (context.Theme.Spacing.TryGetValue(valueSize, out var foundSize))
            size = context.Theme.RemToPxRatio * foundSize;

        if (double.TryParse(valueSize, out foundSize))
            size = context.Theme.RemToPxRatio * foundSize;

        if (!size.HasValue)
            return [];

        return [Layoutable.MarginProperty.ToLiteral(new Thickness(size.Value))];
    }

    [GeneratedRegex(@"^m-(.+)$")]
    private static partial Regex CompiledRegex();
}
