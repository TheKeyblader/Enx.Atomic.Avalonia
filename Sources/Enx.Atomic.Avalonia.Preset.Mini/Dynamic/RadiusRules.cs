using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Enx.Atomic.Avalonia.Composites;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting corner radii from <see cref="IRadiusPart.Radii"/> (<c>rounded-*</c>, <c>rounded-t-*</c>,
/// <c>rounded-r-*</c>, <c>rounded-b-*</c>, <c>rounded-l-*</c>, <c>rounded-tl-*</c>, <c>rounded-tr-*</c>,
/// <c>rounded-br-*</c>, <c>rounded-bl-*</c>).
///
/// <para>
/// Every direction — including the uniform one — writes only <see cref="CornerRadiusSides"/>' side attached
/// properties (<c>double</c>, <c>NaN</c> when unset), in <see cref="CornerRadius"/>'s own slot order
/// (TopLeft, TopRight, BottomRight, BottomLeft); the host's class handler recomposes
/// <see cref="Border.CornerRadiusProperty"/> from them, so <c>rounded-md rounded-tl-none</c> composes instead of
/// the later style replacing the whole struct. Targeting <c>Control</c> explicitly is required twice over: the
/// attached property's <see cref="AvaloniaProperty.OwnerType"/> is the static <see cref="CornerRadiusSides"/> host,
/// and <see cref="Border.CornerRadiusProperty"/> is <c>AddOwner</c>'ed by <see cref="TemplatedControl"/> and
/// <see cref="ContentPresenter"/> — a <c>Border</c>-scoped selector would never reach a <see cref="Button"/>.
/// </para>
/// <para>
/// UnoCSS keeps the uniform, edge (<c>rounded-t-*</c>, two corners) and corner (<c>rounded-tl-*</c>, one corner)
/// forms as separate rule entries in that order; this rule consolidates them into one regex, so
/// <see cref="GetOrderRank"/> reports the order explicitly.
/// </para>
/// </summary>
public partial class RoundedRule<TTheme> : IOrderRankedDynamicRule<TTheme>
    where TTheme : class, IRadiusPart, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!context.Theme.Radii.TryResolve(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return [];

        // The side properties are AttachedProperty<double>; the radius scale is float, so convert explicitly —
        // a boxed float fails the setter's type check once Avalonia applies it.
        var pixels = (double)value;

        return match.Groups["side"].Value switch
        {
            "t" =>
            [
                CornerRadiusSides.CornerRadiusTopLeftProperty.ToLiteral(pixels, typeof(Control)),
                CornerRadiusSides.CornerRadiusTopRightProperty.ToLiteral(pixels, typeof(Control)),
            ],
            "b" =>
            [
                CornerRadiusSides.CornerRadiusBottomRightProperty.ToLiteral(pixels, typeof(Control)),
                CornerRadiusSides.CornerRadiusBottomLeftProperty.ToLiteral(pixels, typeof(Control)),
            ],
            "l" =>
            [
                CornerRadiusSides.CornerRadiusTopLeftProperty.ToLiteral(pixels, typeof(Control)),
                CornerRadiusSides.CornerRadiusBottomLeftProperty.ToLiteral(pixels, typeof(Control)),
            ],
            "r" =>
            [
                CornerRadiusSides.CornerRadiusTopRightProperty.ToLiteral(pixels, typeof(Control)),
                CornerRadiusSides.CornerRadiusBottomRightProperty.ToLiteral(pixels, typeof(Control)),
            ],
            "tl" => [CornerRadiusSides.CornerRadiusTopLeftProperty.ToLiteral(pixels, typeof(Control))],
            "tr" => [CornerRadiusSides.CornerRadiusTopRightProperty.ToLiteral(pixels, typeof(Control))],
            "br" => [CornerRadiusSides.CornerRadiusBottomRightProperty.ToLiteral(pixels, typeof(Control))],
            "bl" => [CornerRadiusSides.CornerRadiusBottomLeftProperty.ToLiteral(pixels, typeof(Control))],
            _ =>
            [
                CornerRadiusSides.CornerRadiusTopLeftProperty.ToLiteral(pixels, typeof(Control)),
                CornerRadiusSides.CornerRadiusTopRightProperty.ToLiteral(pixels, typeof(Control)),
                CornerRadiusSides.CornerRadiusBottomRightProperty.ToLiteral(pixels, typeof(Control)),
                CornerRadiusSides.CornerRadiusBottomLeftProperty.ToLiteral(pixels, typeof(Control)),
            ],
        };
    }

    /// <summary>Uniform beats nothing (rank 0), an edge (<c>rounded-t-*</c>, two corners) overrides its uniform corners (1), a single corner overrides its edge (2).</summary>
    public int GetOrderRank(Match match) =>
        match.Groups["side"].Value switch
        {
            "" => 0,
            "t" or "r" or "b" or "l" => 1,
            _ => 2,
        };

    [GeneratedRegex("^rounded(?:-(?<side>tl|tr|br|bl|t|r|b|l))?-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
