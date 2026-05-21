using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini.Rules;

public class TextElementRules<TTheme>
    where TTheme : MiniTheme
{
    public IRule[] Get() =>
        [
            // FontSizeProperty
            new FontSizeRule<TTheme>(),
            // FontStyleProperty
            new Rule.Static("italic", [TextElement.FontStyleProperty.ToLiteral(FontStyle.Italic)]),
            new Rule.Static(
                "oblique",
                [TextElement.FontStyleProperty.ToLiteral(FontStyle.Oblique)]
            ),
            // FontWeightProperty
            new Rule.Static(
                "font-thin",
                [TextElement.FontWeightProperty.ToLiteral(FontWeight.Thin)]
            ),
            new Rule.Static(
                "font-extralight",
                [TextElement.FontWeightProperty.ToLiteral(FontWeight.ExtraLight)]
            ),
            new Rule.Static(
                "font-light",
                [TextElement.FontWeightProperty.ToLiteral(FontWeight.Light)]
            ),
            new Rule.Static(
                "font-normal",
                [TextElement.FontWeightProperty.ToLiteral(FontWeight.Normal)]
            ),
            new Rule.Static(
                "font-medium",
                [TextElement.FontWeightProperty.ToLiteral(FontWeight.Medium)]
            ),
            new Rule.Static(
                "font-semibold",
                [TextElement.FontWeightProperty.ToLiteral(FontWeight.SemiBold)]
            ),
            new Rule.Static(
                "font-bold",
                [TextElement.FontWeightProperty.ToLiteral(FontWeight.Bold)]
            ),
            new Rule.Static(
                "font-extrabold",
                [TextElement.FontWeightProperty.ToLiteral(FontWeight.ExtraBold)]
            ),
            new Rule.Static(
                "font-black",
                [TextElement.FontWeightProperty.ToLiteral(FontWeight.Black)]
            ),
            new Rule.Static(
                "font-extrablack",
                [TextElement.FontWeightProperty.ToLiteral(FontWeight.ExtraBlack)]
            ),
            // FontStretchProperty
            new Rule.Static(
                "font-stretch-normal",
                [TextElement.FontStretchProperty.ToLiteral(FontStretch.Normal)]
            ),
            new Rule.Static(
                "font-stretch-ultra-condensed",
                [TextElement.FontStretchProperty.ToLiteral(FontStretch.UltraCondensed)]
            ),
            new Rule.Static(
                "font-stretch-extra-condensed",
                [TextElement.FontStretchProperty.ToLiteral(FontStretch.ExtraCondensed)]
            ),
            new Rule.Static(
                "font-stretch-condensed",
                [TextElement.FontStretchProperty.ToLiteral(FontStretch.Condensed)]
            ),
            new Rule.Static(
                "font-stretch-semi-condensed",
                [TextElement.FontStretchProperty.ToLiteral(FontStretch.SemiCondensed)]
            ),
            new Rule.Static(
                "font-stretch-semi-expanded",
                [TextElement.FontStretchProperty.ToLiteral(FontStretch.SemiExpanded)]
            ),
            new Rule.Static(
                "font-stretch-expanded",
                [TextElement.FontStretchProperty.ToLiteral(FontStretch.Expanded)]
            ),
            new Rule.Static(
                "font-stretch-extra-expanded",
                [TextElement.FontStretchProperty.ToLiteral(FontStretch.ExtraExpanded)]
            ),
            new Rule.Static(
                "font-stretch-ultra-condensed",
                [TextElement.FontStretchProperty.ToLiteral(FontStretch.UltraExpanded)]
            ),
            //LetterSpacingProperty
            new LetterSpacingRule<TTheme>(),
        ];
}

public partial record LetterSpacingRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : MiniTheme
{
    public RuleMetadata Metadata { get; } = new();
    public Regex Regex { get; } = GeneratedRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        double? size = null;
        var valueSize = match.Groups[0].Value;
        if (context.Theme.Tracking.TryGetValue(valueSize, out var foundSize))
            size = context.Theme.RemToPxRatio * foundSize;

        if (double.TryParse(valueSize, out foundSize))
            size = context.Theme.RemToPxRatio * foundSize;

        if (!size.HasValue)
            return [];

        return [TextElement.LetterSpacingProperty.ToLiteral(size.Value)];
    }

    [GeneratedRegex("^tracking-(.+)$")]
    public static partial Regex GeneratedRegex();
}

public partial record FontSizeRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : MiniTheme
{
    public RuleMetadata Metadata { get; } = new();
    public Regex Regex { get; } = GeneratedRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        double? size = null;
        double? lineHeight = null;
        var valueSize = match.Groups[0].Value;
        if (context.Theme.TextSize.TryGetValue(valueSize, out var foundSize))
            size = context.Theme.RemToPxRatio * foundSize;

        if (context.Theme.TextSize.TryGetValue(valueSize, out var foundLineHeight))
            lineHeight = context.Theme.RemToPxRatio * foundLineHeight;

        if (double.TryParse(valueSize, out foundSize))
            size = context.Theme.RemToPxRatio * foundSize;

        if (!size.HasValue)
            return [];

        if (lineHeight.HasValue)
            return
            [
                TextElement.FontSizeProperty.ToLiteral(size.Value),
                TextBlock.LineHeightProperty.ToLiteral(lineHeight.Value),
            ];

        return [TextElement.LetterSpacingProperty.ToLiteral(size.Value)];
    }

    [GeneratedRegex("^font-(.+)$")]
    public static partial Regex GeneratedRegex();
}
