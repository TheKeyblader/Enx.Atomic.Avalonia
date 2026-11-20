using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting a <see cref="Layoutable"/> sizing property from <see cref="ISizePart.Sizes"/> (<c>w-*</c>,
/// <c>h-*</c>, <c>min-w-*</c>, <c>max-w-*</c>, <c>min-h-*</c>, <c>max-h-*</c>).
///
/// <para>
/// Avalonia has no percentage lengths, so the self-sizing keywords UnoCSS resolves outside the scale are mapped
/// explicitly: <c>w-full</c>/<c>h-full</c> clear <see cref="Layoutable.WidthProperty"/>/<see cref="Layoutable.HeightProperty"/>
/// to <see cref="double.NaN"/> (“size to what the parent offers”) <em>and</em> set
/// <see cref="Layoutable.HorizontalAlignmentProperty"/>/<see cref="Layoutable.VerticalAlignmentProperty"/> to
/// <see cref="HorizontalAlignment.Stretch"/>/<see cref="VerticalAlignment.Stretch"/>, which is the closest Avalonia
/// equivalent of CSS's <c>width: 100%</c>; <c>w-auto</c>/<c>h-auto</c> only clear the length, leaving the alignment
/// untouched. This fills the space a parent panel offers (a <see cref="Avalonia.Controls.Grid"/> cell,
/// <see cref="Avalonia.Controls.DockPanel"/>, or a <see cref="Avalonia.Controls.StackPanel"/>'s cross axis) but can't
/// express “100% of the main axis” inside a <see cref="Avalonia.Controls.StackPanel"/>, which sizes its children to
/// their desired size on that axis.
/// </para>
/// <para>
/// The keyword forms share the rule with the scale values, so <see cref="GetOrderRank"/> fixes their relative order
/// instead of leaving it to source order: <c>auto</c> ranks below a concrete size and <c>full</c> above it, matching
/// the order Tailwind emits its width utilities in (<c>w-auto</c>, then the spacing scale, then <c>w-full</c>). So
/// <c>w-4 w-full</c> and <c>w-full w-4</c> both end up filling, and <c>w-auto w-4</c> keeps <c>w-4</c>'s width
/// whatever the source order.
/// </para>
/// </summary>
public partial class SizeRule<TTheme> : IOrderRankedDynamicRule<TTheme>
    where TTheme : class, ISizePart, IRemToPxPart
{
    /// <summary>Rank of a self-sizing keyword below a concrete size (<c>w-auto</c>).</summary>
    private const int AutoRank = 0;

    /// <summary>Rank of a concrete size resolved from the scale, a bare rem number or an arbitrary value.</summary>
    private const int SizeRank = 1;

    /// <summary>Rank of a self-sizing keyword above a concrete size (<c>w-full</c>).</summary>
    private const int FullRank = 2;

    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        var bound = match.Groups["bound"].Value;
        var axis = match.Groups["axis"].Value;
        var key = match.Groups["value"].Value;

        if (bound.Length == 0 && key is "full" or "auto")
        {
            var property = axis == "w" ? Layoutable.WidthProperty : Layoutable.HeightProperty;
            if (key == "auto")
                return [property.ToLiteral(double.NaN)];

            StyleValue alignment =
                axis == "w"
                    ? Layoutable.HorizontalAlignmentProperty.ToLiteral(HorizontalAlignment.Stretch)
                    : Layoutable.VerticalAlignmentProperty.ToLiteral(VerticalAlignment.Stretch);
            return [property.ToLiteral(double.NaN), alignment];
        }

        if (!context.Theme.Sizes.TryResolve(key, context.Theme.RemToPxFactor, out var value))
            return [];

        var resolved = (bound, axis) switch
        {
            ("min-", "w") => Layoutable.MinWidthProperty,
            ("max-", "w") => Layoutable.MaxWidthProperty,
            ("min-", "h") => Layoutable.MinHeightProperty,
            ("max-", "h") => Layoutable.MaxHeightProperty,
            (_, "w") => Layoutable.WidthProperty,
            _ => Layoutable.HeightProperty,
        };

        return [resolved.ToLiteral(value)];
    }

    /// <summary>
    /// A bound (<c>min-*</c>/<c>max-*</c>) targets its own property, so it keeps the neutral size rank; among the
    /// unbounded forms <c>auto</c> sits below a concrete size and <c>full</c> above it — see the type's remarks.
    /// </summary>
    public int GetOrderRank(Match match) =>
        match.Groups["bound"].Value.Length > 0 ? SizeRank
        : match.Groups["value"].Value == "full" ? FullRank
        : match.Groups["value"].Value == "auto" ? AutoRank
        : SizeRank;

    [GeneratedRegex("^(?<bound>min-|max-)?(?<axis>w|h)-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
