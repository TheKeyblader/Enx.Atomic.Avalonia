using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="Visual.ZIndexProperty"/> (<c>z-10</c>, <c>z10</c>, <c>z-[5]</c>) — UnoCSS's
/// <c>zIndexes</c> family, in both its hyphenated and its compact spelling.
///
/// <para>
/// Avalonia's property is an <c>int</c>, not a CSS number: a non-integral value (<c>z-1.5</c>) is treated as a miss
/// rather than silently rounded, so a token never resolves to a value it didn't ask for. A negative index is
/// representable and accepted through the arbitrary form (<c>z-[-1]</c>), but not through the compact <c>-z-*</c>
/// spelling — this port has no generic negation variant (the leading <c>-</c> is implemented inside
/// <see cref="MarginRule{TTheme}"/> for margins only).
/// </para>
/// <para>
/// Upstream's only <c>zIndex</c> theme entry, <c>z-auto</c>, is deliberately not ported: CSS <c>z-index: auto</c>
/// means "stack as if no z-index were set", which an <c>int</c> cannot express — writing <c>0</c> would instead
/// override a value another style sets. There is therefore no <c>zIndex</c> scale here, only the numeric tokens.
/// </para>
/// </summary>
public partial class ZIndexRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        var raw = match.Groups["value"].Value;
        var text = ArbitraryValue.TryUnwrap(raw, out var arbitrary) ? arbitrary : raw;

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return [];

        return [Visual.ZIndexProperty.ToLiteral(value)];
    }

    [GeneratedRegex("^z-?(?<value>\\d+|\\[.+\\])$")]
    private static partial Regex CompiledRegex();
}
