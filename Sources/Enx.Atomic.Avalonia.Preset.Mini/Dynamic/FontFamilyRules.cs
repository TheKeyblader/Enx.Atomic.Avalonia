using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia.Controls.Documents;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="TextElement.FontFamilyProperty"/> from <see cref="IFontFamilyPart.FontFamilies"/>
/// (<c>font-sans</c>, <c>font-serif</c>, <c>font-mono</c>) — UnoCSS's <c>font-{family}</c> family.
///
/// <para>
/// The <c>font-</c> prefix is shared with the static font-weight rules (<c>font-bold</c>, <c>font-thin</c>, ...): a
/// static rule always wins over a dynamic one, so those tokens keep setting
/// <see cref="TextElement.FontWeightProperty"/> and this rule only fires when the value is a key of the font-family
/// scale (see <c>AmbiguousPrefixTests</c>). Upstream's arbitrary form (<c>font-[Arial]</c>) is deliberately not
/// ported: an arbitrary value here would be text, whereas every other rule's bracket form is a numeric pixel
/// literal.
/// </para>
/// <para>
/// The property is an inherited attached property, so the default <see cref="StyleValue.TargetType"/>
/// (<c>StyledElement</c>) reaches a <c>TextBlock</c>, a <c>TextBox</c>, and anything else hosting text — one style,
/// no per-owner duplication.
/// </para>
/// </summary>
public partial class FontFamilyRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, IFontFamilyPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!context.Theme.FontFamilies.TryGetValue(match.Groups["value"].Value, out var family))
            return [];

        return [TextElement.FontFamilyProperty.ToLiteral(family)];
    }

    [GeneratedRegex("^font-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
