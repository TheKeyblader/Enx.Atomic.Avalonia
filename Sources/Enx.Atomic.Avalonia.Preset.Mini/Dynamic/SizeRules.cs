using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting a <see cref="Layoutable"/> sizing property from <see cref="ISizePart.Sizes"/> (<c>w-*</c>,
/// <c>h-*</c>, <c>min-w-*</c>, <c>max-w-*</c>, <c>min-h-*</c>, <c>max-h-*</c>).
/// </summary>
public partial class SizeRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, ISizePart, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!context.Theme.Sizes.TryResolve(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return [];

        var v = (double)value;
        var property = (match.Groups["bound"].Value, match.Groups["axis"].Value) switch
        {
            ("min-", "w") => Layoutable.MinWidthProperty,
            ("max-", "w") => Layoutable.MaxWidthProperty,
            ("min-", "h") => Layoutable.MinHeightProperty,
            ("max-", "h") => Layoutable.MaxHeightProperty,
            (_, "w") => Layoutable.WidthProperty,
            _ => Layoutable.HeightProperty,
        };

        return [property.ToLiteral(v)];
    }

    [GeneratedRegex("^(?<bound>min-|max-)?(?<axis>w|h)-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
