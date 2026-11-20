using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="TextBlock.LineHeightProperty"/> (<c>leading-*</c>, <c>lh-*</c>,
/// <c>line-height-*</c>) — UnoCSS's <c>typography.ts</c> line-height family.
///
/// <para>
/// Avalonia's <c>LineHeight</c> is an <em>absolute</em> pixel height (<c>double.NaN</c> meaning "derive it from the
/// font metrics"), so only the absolute forms are ported: an arbitrary pixel value (<c>leading-[24px]</c>) or a bare
/// number in quarter-rems (<c>leading-4</c> = 1rem = 16px, UnoCSS's bare-number convention). Upstream's named entries
/// (<c>leading-none</c> = 1, <c>leading-tight</c> = 1.25, <c>leading-normal</c> = 1.5, …) are unitless multipliers,
/// and a style cannot multiply an absolute height by the element's resolved <c>FontSize</c> at application time, so
/// they are deliberately dropped rather than baked against a guessed font size — see
/// <see cref="ThemeScale.TryResolveUnscaled"/>.
/// </para>
/// <para>
/// The property is an inherited attached property declared on <see cref="TextBlock"/> for any <see cref="Control"/>,
/// so the default <see cref="StyleValue.TargetType"/> (<c>StyledElement</c>) lets one style reach a <c>TextBlock</c>,
/// a <c>TextBox</c>, or any control that hosts text.
/// </para>
/// </summary>
public partial class LineHeightRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!ThemeScale.TryResolveUnscaled(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return [];

        return [TextBlock.LineHeightProperty.ToLiteral(value)];
    }

    [GeneratedRegex("^(?:leading|lh|line-height)-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
