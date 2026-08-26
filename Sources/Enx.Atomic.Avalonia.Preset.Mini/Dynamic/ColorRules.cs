using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Parses an <see cref="ArbitraryValue"/> (<c>[#ff0000]</c>) shared by every color rule below — always a
/// <see cref="StyleValue.Literal{TValue}"/>, never a <see cref="StyleValue.Resource"/>, since an arbitrary value
/// has no named theme entry for a <c>ThemeAccess</c> expression to point at.
/// </summary>
file static class ArbitraryColor
{
    public static bool TryParse(string value, out IBrush brush)
    {
        if (ArbitraryValue.TryUnwrap(value, out var content) && Color.TryParse(content, out var color))
        {
            brush = new SolidColorBrush(color);
            return true;
        }

        brush = null!;
        return false;
    }
}

/// <summary>Dynamic rule setting <see cref="Border.BackgroundProperty"/> from <see cref="IColorPart.Colors"/> (<c>bg-*</c>).</summary>
public partial class BackgroundColorRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, IColorPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        var value = match.Groups["value"].Value;

        // Border.BackgroundProperty and TemplatedControl.BackgroundProperty are the exact same AvaloniaProperty
        // instance (TemplatedControl adds itself as an owner of Border's property) — its OwnerType always
        // reports Border, the original registrant, regardless of which static field this was accessed through.
        // Without an explicit targetType here, this second entry would collapse into the same group as the one
        // above and never produce a selector matching Button/ComboBox/etc., which derive from TemplatedControl,
        // not Border.
        if (ArbitraryColor.TryParse(value, out var arbitraryBrush))
            return
            [
                Border.BackgroundProperty.ToLiteral(arbitraryBrush),
                TemplatedControl.BackgroundProperty.ToLiteral(arbitraryBrush, typeof(TemplatedControl)),
                Panel.BackgroundProperty.ToLiteral(arbitraryBrush, typeof(Panel)),
            ];

        if (!context.Theme.Colors.ContainsKey(value))
            return [];

        return
        [
            Border.BackgroundProperty.ToResource((TTheme t) => t.Colors[value]),
            TemplatedControl.BackgroundProperty.ToResource((TTheme t) => t.Colors[value], typeof(TemplatedControl)),
            Panel.BackgroundProperty.ToResource((TTheme t) => t.Colors[value], typeof(Panel)),
        ];
    }

    [GeneratedRegex("^bg-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}

/// <summary>
/// Dynamic rule setting <see cref="TextElement.ForegroundProperty"/> from <see cref="IColorPart.Colors"/> (<c>text-*</c>).
/// Tried after any <c>text-*</c> font-size rule, since both share the <c>text-</c> prefix.
/// </summary>
public partial class ForegroundColorRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, IColorPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        var value = match.Groups["value"].Value;

        if (ArbitraryColor.TryParse(value, out var arbitraryBrush))
            return [TextElement.ForegroundProperty.ToLiteral(arbitraryBrush)];

        if (!context.Theme.Colors.ContainsKey(value))
            return [];

        return [TextElement.ForegroundProperty.ToResource((TTheme t) => t.Colors[value])];
    }

    [GeneratedRegex("^text-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}

/// <summary>
/// Dynamic rule setting <see cref="Border.BorderBrushProperty"/> from <see cref="IColorPart.Colors"/> (<c>border-*</c>).
/// Tried after any <c>border-*</c> border-width rule, since both share the <c>border-</c> prefix.
/// </summary>
public partial class BorderColorRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, IColorPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        var value = match.Groups["value"].Value;

        if (ArbitraryColor.TryParse(value, out var arbitraryBrush))
            return [Border.BorderBrushProperty.ToLiteral(arbitraryBrush)];

        if (!context.Theme.Colors.ContainsKey(value))
            return [];

        return [Border.BorderBrushProperty.ToResource((TTheme t) => t.Colors[value])];
    }

    [GeneratedRegex("^border-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
