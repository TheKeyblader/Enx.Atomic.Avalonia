using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="Border.BorderThicknessProperty"/> from <see cref="ILineWidthPart.LineWidths"/>, uniformly
/// on all four sides (<c>border-*</c>). Tried before <see cref="BorderColorRule{TTheme}"/>, since both share the
/// <c>border-</c> prefix — a value that resolves in the line-width scale is treated as a width, otherwise it falls
/// through to the color rule. Unmatched bare numbers fall back to px (not rem, unlike the other scales) — mirroring
/// UnoCSS, where <c>lineWidth</c> is the one scale whose raw-number fallback stays in pixels.
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

        return [Border.BorderThicknessProperty.ToLiteral(new Thickness(value))];
    }

    [GeneratedRegex("^border(?:-(?<value>.+))?$")]
    private static partial Regex CompiledRegex();
}
