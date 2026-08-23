using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia.Controls.Documents;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Dynamic rule setting <see cref="TextElement.FontSizeProperty"/> from <see cref="IFontSizePart.FontSizes"/> (<c>text-*</c>).
/// Tried before <see cref="ForegroundColorRule{TTheme}"/>, since both share the <c>text-</c> prefix — a value that
/// resolves in the font-size scale is treated as a size, otherwise it falls through to the color rule.
/// </summary>
public partial class FontSizeRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, IFontSizePart, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!context.Theme.FontSizes.TryResolve(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return [];

        return [TextElement.FontSizeProperty.ToLiteral(value)];
    }

    [GeneratedRegex("^text-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
