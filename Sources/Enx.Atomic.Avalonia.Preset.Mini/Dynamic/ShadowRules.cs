using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="Border.BoxShadowProperty"/> from <see cref="IBoxShadowPart.BoxShadows"/>
/// (<c>shadow</c>, <c>shadow-sm</c>, <c>shadow-md</c>, <c>shadow-lg</c>, <c>shadow-xl</c>, <c>shadow-2xl</c>,
/// <c>shadow-inner</c>, <c>shadow-none</c>) — the size half of UnoCSS's <c>_rules/shadow.ts</c> only: shadow
/// colors (<c>shadow-red-500</c>), <c>shadow-inset</c> as a modifier, <c>shadow-opacity-*</c> and arbitrary
/// values are deliberately not ported, since Avalonia's <see cref="BoxShadow"/> carries its color inline and has
/// no separate opacity channel. A token whose suffix isn't a scale key is a miss, so it resolves to nothing.
///
/// <para>
/// Two styles per token, like <c>bg-*</c>: the exact same property instance is shared between
/// <see cref="Border"/> and <see cref="ContentPresenter"/> (<c>AddOwner</c>), and <see cref="AvaloniaProperty.OwnerType"/>
/// always reports <see cref="Border"/>, so the <see cref="ContentPresenter"/> entry needs an explicit target type
/// or it would collapse into the same style. A Fluent <c>Button</c>'s template doesn't forward its own
/// <see cref="BoxShadow"/> to the shadow-rendering template part, so a shadow utility is only visibly effective on
/// a <see cref="Border"/> or a bare <see cref="ContentPresenter"/>.
/// </para>
/// <para>
/// Values are <see cref="StyleValue.Literal{TValue}"/>, never resources: unlike a color scale entry, the shadow
/// value isn't a Light/Dark pair and needs no <c>DynamicResource</c> indirection.
/// </para>
/// </summary>
public partial class ShadowRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, IBoxShadowPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        var key = match.Groups["value"].Success ? match.Groups["value"].Value : "DEFAULT";
        if (!context.Theme.BoxShadows.TryGetValue(key, out var shadows))
            return [];

        return
        [
            Border.BoxShadowProperty.ToLiteral(shadows),
            ContentPresenter.BoxShadowProperty.ToLiteral(shadows, typeof(ContentPresenter)),
        ];
    }

    [GeneratedRegex("^shadow(?:-(?<value>.+))?$")]
    private static partial Regex CompiledRegex();
}
