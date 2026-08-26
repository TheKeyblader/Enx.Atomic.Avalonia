using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>Dynamic rule setting <see cref="Border.BackgroundProperty"/> from <see cref="IColorPart.Colors"/> (<c>bg-*</c>).</summary>
public partial class BackgroundColorRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, IColorPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        var value = match.Groups["value"].Value;
        if (!context.Theme.Colors.ContainsKey(value))
            return [];

        return
        [
            Border.BackgroundProperty.ToResource((TTheme t) => t.Colors[value]),
            // Border.BackgroundProperty and TemplatedControl.BackgroundProperty are the exact same
            // AvaloniaProperty instance (TemplatedControl adds itself as an owner of Border's property) — its
            // OwnerType always reports Border, the original registrant, regardless of which static field this
            // was accessed through. Without an explicit targetType here, this second entry would collapse into
            // the same group as the one above and never produce a selector matching Button/ComboBox/etc., which
            // derive from TemplatedControl, not Border.
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
        if (!context.Theme.Colors.ContainsKey(value))
            return [];

        return [Border.BorderBrushProperty.ToResource((TTheme t) => t.Colors[value])];
    }

    [GeneratedRegex("^border-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
