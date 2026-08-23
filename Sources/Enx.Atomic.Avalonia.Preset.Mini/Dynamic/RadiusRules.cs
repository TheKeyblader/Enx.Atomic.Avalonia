using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting corner radii from <see cref="IRadiusPart.Radii"/> (<c>rounded-*</c>, <c>rounded-t-*</c>,
/// <c>rounded-r-*</c>, <c>rounded-b-*</c>, <c>rounded-l-*</c>, <c>rounded-tl-*</c>, <c>rounded-tr-*</c>,
/// <c>rounded-br-*</c>, <c>rounded-bl-*</c>). The uniform variant (<c>rounded-*</c>, no side) sets
/// <see cref="Border.CornerRadiusProperty"/> directly; every other variant targets <see cref="SpecialProperties"/>
/// ghost properties instead of zeroing the corners it doesn't cover — see <see cref="GhostPropertyCombiner{TTheme}"/>.
/// </summary>
public partial class RoundedRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, IRadiusPart, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!context.Theme.Radii.TryResolve(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return [];

        return match.Groups["side"].Value switch
        {
            "t" =>
            [
                SpecialProperties.CornerRadiusTopLeftProperty.ToLiteral(value),
                SpecialProperties.CornerRadiusTopRightProperty.ToLiteral(value),
            ],
            "b" =>
            [
                SpecialProperties.CornerRadiusBottomRightProperty.ToLiteral(value),
                SpecialProperties.CornerRadiusBottomLeftProperty.ToLiteral(value),
            ],
            "l" =>
            [
                SpecialProperties.CornerRadiusTopLeftProperty.ToLiteral(value),
                SpecialProperties.CornerRadiusBottomLeftProperty.ToLiteral(value),
            ],
            "r" =>
            [
                SpecialProperties.CornerRadiusTopRightProperty.ToLiteral(value),
                SpecialProperties.CornerRadiusBottomRightProperty.ToLiteral(value),
            ],
            "tl" => [SpecialProperties.CornerRadiusTopLeftProperty.ToLiteral(value)],
            "tr" => [SpecialProperties.CornerRadiusTopRightProperty.ToLiteral(value)],
            "br" => [SpecialProperties.CornerRadiusBottomRightProperty.ToLiteral(value)],
            "bl" => [SpecialProperties.CornerRadiusBottomLeftProperty.ToLiteral(value)],
            _ => [Border.CornerRadiusProperty.ToLiteral(new CornerRadius(value))],
        };
    }

    [GeneratedRegex("^rounded(?:-(?<side>tl|tr|br|bl|t|r|b|l))?-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
