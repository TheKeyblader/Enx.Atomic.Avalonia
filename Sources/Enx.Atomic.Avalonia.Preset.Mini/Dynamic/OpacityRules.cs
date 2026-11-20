using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="Visual.OpacityProperty"/> from a percentage (<c>opacity-50</c>, <c>op-25</c>) or an
/// arbitrary value (<c>opacity-[0.35]</c>, <c>opacity-[50%]</c>) — UnoCSS's <c>opacity.ts</c> family, also reachable
/// through its <c>op-*</c> alias.
///
/// <para>
/// UnoCSS's <c>h.bracket.percent</c> handler reads a bare token as a percentage, so <c>opacity-50</c> is 0.5 and
/// <c>opacity-100</c> is 1 — <c>opacity-0.5</c> is therefore half a percent (0.005), not 0.5, exactly as upstream.
/// An arbitrary value bypasses that conversion and is taken as written, matching the bracket escape hatch's "use
/// exactly this" meaning: <c>opacity-[0.35]</c> is a fraction, while <c>opacity-[50%]</c> spells the percentage out.
/// </para>
/// <para>
/// Avalonia's property is a plain <c>double</c> with no validation and CSS has no invalid-value concept here either,
/// so a value outside 0–1 (e.g. <c>opacity-[2]</c>) is passed through rather than clamped.
/// </para>
/// </summary>
public partial class OpacityRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        var raw = match.Groups["value"].Value;

        if (ArbitraryValue.TryUnwrap(raw, out var arbitrary))
        {
            if (!TryParseNumber(arbitrary, out var literal))
                return [];

            return [Visual.OpacityProperty.ToLiteral(literal)];
        }

        // A bare token is a percentage, not a fraction — see the type's remarks.
        if (!TryParseNumber(raw, out var percent))
            return [];

        return [Visual.OpacityProperty.ToLiteral(percent / 100d)];
    }

    /// <summary>Parses an optional trailing <c>%</c> into a 0–1 fraction; a plain number is returned as-is (no division).</summary>
    private static bool TryParseNumber(string text, out double value)
    {
        var percent = text.EndsWith('%');
        var raw = percent ? text[..^1] : text;
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return false;

        if (percent)
            value /= 100d;

        return true;
    }

    [GeneratedRegex("^(?:opacity|op)-?(?<value>\\[.+\\]|[\\d.]+%?)$")]
    private static partial Regex CompiledRegex();
}
