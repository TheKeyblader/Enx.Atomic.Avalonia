using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia.Controls.Documents;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="TextElement.LetterSpacingProperty"/> (<c>tracking-*</c>) — UnoCSS's
/// <c>typography.ts</c> letter-spacing family.
///
/// <para>
/// Avalonia's <c>LetterSpacing</c> is documented and implemented in <em>pixels</em> (it feeds the glyph shaper as an
/// absolute advance adjustment), while every upstream <c>letterSpacing</c> entry is in <c>em</c> — relative to the
/// element's own resolved font size, which a style cannot read. So only the absolute forms are ported: an arbitrary
/// pixel value (<c>tracking-[2px]</c>) or a bare number in quarter-rems (<c>tracking-4</c> = 1rem = 16px,
/// UnoCSS's bare-number convention). The named scale (<c>tracking-tight</c>, <c>tracking-wide</c>, …) is therefore dropped
/// rather than baked against a guessed font size — see <see cref="ThemeScale.TryResolveUnscaled"/>.
/// </para>
/// </summary>
public partial class LetterSpacingRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!ThemeScale.TryResolveUnscaled(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return [];

        return [TextElement.LetterSpacingProperty.ToLiteral(value)];
    }

    [GeneratedRegex("^tracking-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
