using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="Border.BorderThicknessProperty"/> from <see cref="ILineWidthPart.LineWidths"/>,
/// either uniformly on all four sides (<c>border-*</c>) or on a single side (<c>border-t-*</c>, <c>border-r-*</c>,
/// <c>border-b-*</c>, <c>border-l-*</c>). Tried before <see cref="BorderColorRule{TTheme}"/>, since both share the
/// <c>border-</c> prefix — a value that resolves in the line-width scale is treated as a width, otherwise it falls
/// through to the color rule. Unmatched bare numbers fall back to px (not rem, unlike the other scales) — mirroring
/// UnoCSS, where <c>lineWidth</c> is the one scale whose raw-number fallback stays in pixels. The per-side variants
/// target <see cref="SpecialProperties"/> ghost properties instead of zeroing the sides they don't cover — see
/// <see cref="GhostPropertyCombiner{TTheme}"/>, which is what turns them into a real
/// <see cref="Border.BorderThicknessProperty"/> value.
/// </summary>
public partial class BorderWidthRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, ILineWidthPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        var key = match.Groups["value"].Success ? match.Groups["value"].Value : "DEFAULT";
        if (!context.Theme.LineWidths.TryResolvePx(key, out var value))
            return [];

        return match.Groups["side"].Value switch
        {
            "t" => [SpecialProperties.BorderThicknessTopProperty.ToLiteral(value)],
            "r" => [SpecialProperties.BorderThicknessRightProperty.ToLiteral(value)],
            "b" => [SpecialProperties.BorderThicknessBottomProperty.ToLiteral(value)],
            "l" => [SpecialProperties.BorderThicknessLeftProperty.ToLiteral(value)],
            _ => [Border.BorderThicknessProperty.ToLiteral(new Thickness(value))],
        };
    }

    [GeneratedRegex("^border(?:-(?<side>[trbl])(?:-(?<value>.+))?|-(?<value>.+))?$")]
    private static partial Regex CompiledRegex();
}
