using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="Border.CornerRadiusProperty"/> from <see cref="IRadiusPart.Radii"/> (<c>rounded-*</c>,
/// <c>rounded-t-*</c>, <c>rounded-r-*</c>, <c>rounded-b-*</c>, <c>rounded-l-*</c>, <c>rounded-tl-*</c>, <c>rounded-tr-*</c>,
/// <c>rounded-br-*</c>, <c>rounded-bl-*</c>). Setting a single side/corner zeroes the corners not covered, since
/// <see cref="CornerRadius"/> is one struct-valued property rather than four independent ones.
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

        var radius = match.Groups["side"].Value switch
        {
            "t" => new CornerRadius(value, 0),
            "b" => new CornerRadius(0, value),
            "l" => new CornerRadius(value, 0, 0, value),
            "r" => new CornerRadius(0, value, value, 0),
            "tl" => new CornerRadius(value, 0, 0, 0),
            "tr" => new CornerRadius(0, value, 0, 0),
            "br" => new CornerRadius(0, 0, value, 0),
            "bl" => new CornerRadius(0, 0, 0, value),
            _ => new CornerRadius(value),
        };

        return [Border.CornerRadiusProperty.ToLiteral(radius)];
    }

    [GeneratedRegex("^rounded(?:-(?<side>tl|tr|br|bl|t|r|b|l))?-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
