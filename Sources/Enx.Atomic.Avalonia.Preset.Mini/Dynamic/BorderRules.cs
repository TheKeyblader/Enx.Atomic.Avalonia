using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Enx.Atomic.Avalonia.Composites;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting border widths from <see cref="ILineWidthPart.LineWidths"/>, either uniformly on all four
/// sides (<c>border</c>, <c>border-*</c>) or on a single side (<c>border-t-*</c>, <c>border-r-*</c>,
/// <c>border-b-*</c>, <c>border-l-*</c>). Tried before <see cref="BorderColorRule{TTheme}"/>, since both share the
/// <c>border-</c> prefix — a value that resolves in the line-width scale is treated as a width, otherwise it falls
/// through to the color rule. Unmatched bare numbers fall back to px (not rem, unlike the other scales) — mirroring
/// UnoCSS, where <c>lineWidth</c> is the one scale whose raw-number fallback stays in pixels.
///
/// <para>
/// Every direction — including the uniform one — writes only <see cref="BorderThicknessSides"/>' side attached
/// properties (<c>double</c>, <c>NaN</c> when unset); the host's class handler recomposes
/// <see cref="Border.BorderThicknessProperty"/> from them, so <c>border-2 border-t-4</c> composes instead of the
/// later style replacing the whole <see cref="Thickness"/>. Targeting <c>Control</c> explicitly is required twice
/// over: the attached property's <see cref="AvaloniaProperty.OwnerType"/> is the static
/// <see cref="BorderThicknessSides"/> host (a meaningless <c>Is&lt;BorderThicknessSides&gt;</c> selector), and
/// <see cref="Border.BorderThicknessProperty"/> is <c>AddOwner</c>'ed by <see cref="TemplatedControl"/> and
/// <see cref="ContentPresenter"/> — a <c>Border</c>-scoped selector would never reach a <see cref="Button"/>.
/// </para>
/// <para>
/// UnoCSS splits uniform and per-side widths into separate rule entries with increasing indices; this rule
/// consolidates them into one regex, so <see cref="GetOrderRank"/> reports that order explicitly —
/// <c>border-2 border-t-4</c> must let the side win whatever the source order.
/// </para>
/// </summary>
public partial class BorderWidthRule<TTheme> : IOrderRankedDynamicRule<TTheme>
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
            "t" => [BorderThicknessSides.BorderThicknessTopProperty.ToLiteral(value, typeof(Control))],
            "r" => [BorderThicknessSides.BorderThicknessRightProperty.ToLiteral(value, typeof(Control))],
            "b" => [BorderThicknessSides.BorderThicknessBottomProperty.ToLiteral(value, typeof(Control))],
            "l" => [BorderThicknessSides.BorderThicknessLeftProperty.ToLiteral(value, typeof(Control))],
            _ =>
            [
                BorderThicknessSides.BorderThicknessLeftProperty.ToLiteral(value, typeof(Control)),
                BorderThicknessSides.BorderThicknessTopProperty.ToLiteral(value, typeof(Control)),
                BorderThicknessSides.BorderThicknessRightProperty.ToLiteral(value, typeof(Control)),
                BorderThicknessSides.BorderThicknessBottomProperty.ToLiteral(value, typeof(Control)),
            ],
        };
    }

    /// <summary>Uniform beats nothing (rank 0); a single side overrides the uniform sides it covers (1) — UnoCSS's uniform entry comes first.</summary>
    public int GetOrderRank(Match match) => match.Groups["side"].Value.Length == 0 ? 0 : 1;

    [GeneratedRegex("^border(?:-(?<side>[trbl])(?:-(?<value>.+))?|-(?<value>.+))?$")]
    private static partial Regex CompiledRegex();
}
