using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="Canvas"/>'s placement attached properties from <see cref="ISpacingPart.Spacing"/>
/// (<c>inset-*</c>, <c>inset-x-*</c>, <c>inset-y-*</c>, <c>inset-t/r/b/l-*</c>, and the equivalent
/// <c>top-*</c>/<c>right-*</c>/<c>bottom-*</c>/<c>left-*</c>). A leading <c>-</c> negates the resolved spacing, the
/// same way <see cref="MarginRule{TTheme}"/> handles <c>-m-*</c> (this preset has no generic negation variant).
///
/// <para>
/// Avalonia has neither <c>position</c> nor an <c>inset</c> property: absolute placement is <see cref="Canvas"/>'s job,
/// arranging a child from <see cref="Canvas.LeftProperty"/>/<see cref="Canvas.TopProperty"/> — and from
/// <see cref="Canvas.RightProperty"/>/<see cref="Canvas.BottomProperty"/> when the left/top side is unset. These
/// tokens are therefore only meaningful on a child of a <see cref="Canvas"/>. A uniform <c>inset-*</c> writes all four
/// sides, as upstream's <c>inset</c> does; the side values are the spacing scale's pixels, so upstream's
/// <c>auto</c>/fraction forms are not ported (each side is a plain <c>double</c>, <c>NaN</c> meaning "unset").
/// </para>
/// <para>
/// The uniform/axis/side forms are one regex rather than UnoCSS's separate rule entries, so
/// <see cref="GetOrderRank"/> reports their order (uniform &lt; axis &lt; side): <c>inset-0 top-4</c> lets the side
/// win whatever the source order.
/// </para>
/// </summary>
public partial class InsetRule<TTheme> : IOrderRankedDynamicRule<TTheme>
    where TTheme : class, ISpacingPart, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        var family = match.Groups["family"].Value;
        var direction = match.Groups["direction"].Value;

        // A dedicated side token (top-4) takes no second direction segment; the shared regex would otherwise accept
        // `top-x-4` and silently read it as `top-4`.
        if (family != "inset" && direction.Length > 0)
            return [];

        if (!context.Theme.Spacing.TryResolve(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return [];

        if (match.Groups["neg"].Success)
            value = -value;

        return (family, direction) switch
        {
            ("top", _) => [Canvas.TopProperty.ToLiteral(value)],
            ("right", _) => [Canvas.RightProperty.ToLiteral(value)],
            ("bottom", _) => [Canvas.BottomProperty.ToLiteral(value)],
            ("left", _) => [Canvas.LeftProperty.ToLiteral(value)],
            (_, "x") => [Canvas.LeftProperty.ToLiteral(value), Canvas.RightProperty.ToLiteral(value)],
            (_, "y") => [Canvas.TopProperty.ToLiteral(value), Canvas.BottomProperty.ToLiteral(value)],
            (_, "t") => [Canvas.TopProperty.ToLiteral(value)],
            (_, "r") => [Canvas.RightProperty.ToLiteral(value)],
            (_, "b") => [Canvas.BottomProperty.ToLiteral(value)],
            (_, "l") => [Canvas.LeftProperty.ToLiteral(value)],
            _ =>
            [
                Canvas.LeftProperty.ToLiteral(value),
                Canvas.TopProperty.ToLiteral(value),
                Canvas.RightProperty.ToLiteral(value),
                Canvas.BottomProperty.ToLiteral(value),
            ],
        };
    }

    /// <summary>
    /// A dedicated side token or an <c>inset-{side}</c> segment overrides its axis (2), which overrides the uniform
    /// <c>inset-*</c> (1); the uniform form beats nothing (0). Mirrors UnoCSS's increasing rule entries.
    /// </summary>
    public int GetOrderRank(Match match)
    {
        if (match.Groups["family"].Value != "inset")
            return 2;

        return match.Groups["direction"].Value switch
        {
            "" => 0,
            "x" or "y" => 1,
            _ => 2,
        };
    }

    [GeneratedRegex("^(?<neg>-)?(?<family>inset|top|right|bottom|left)(?:-(?<direction>[xytrbl]))?-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
