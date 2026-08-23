using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia.Preset.Mini.Variants;

/// <summary>
/// Matches a Tailwind/UnoCSS-style pseudo-class prefix (<c>hover:</c>, <c>disabled:</c>, ...) and rewrites it to
/// the corresponding Avalonia pseudo-class (<c>:pointerover</c>, <c>:disabled</c>, ...), appended to the resolved
/// style's selector. Pruned to the state pseudo-classes Avalonia controls actually raise — unlike CSS, Avalonia
/// has no structural (<c>:nth-child</c>) or content (<c>:empty</c>) pseudo-classes to mirror.
/// </summary>
public record PseudoClassVariant<TTheme> : VariantBase<TTheme>
    where TTheme : class
{
    private static readonly Dictionary<string, string> PseudoClasses = new()
    {
        ["hover"] = ":pointerover",
        ["active"] = ":pressed",
        ["pressed"] = ":pressed",
        ["disabled"] = ":disabled",
        ["enabled"] = ":enabled",
        ["focus"] = ":focus",
        ["focus-visible"] = ":focus-visible",
        ["focus-within"] = ":focus-within",
        ["selected"] = ":selected",
        ["checked"] = ":checked",
        ["unchecked"] = ":unchecked",
        ["indeterminate"] = ":indeterminate",
        ["dragging"] = ":dragging",
        ["empty"] = ":empty",
        ["open"] = ":open",
        ["invalid"] = ":invalid",
        ["readonly"] = ":readonly",
    };

    public PseudoClassVariant() => MultiPass = true;

    public override VariantHandlerBase[] Match(string matcher, VariantContext<TTheme> context)
    {
        foreach (var (name, pseudoClass) in PseudoClasses)
        {
            var prefix = $"{name}:";
            if (!matcher.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            return [new PseudoClassVariantHandler { Matcher = matcher[prefix.Length..], PseudoClass = pseudoClass }];
        }

        return [];
    }
}

/// <summary>Appends a fixed Avalonia pseudo-class to the resolved style's selector.</summary>
public record PseudoClassVariantHandler : VariantHandlerBase
{
    public required string PseudoClass { get; init; }

    public override VariantHandlerContext Handle(
        VariantHandlerContext input,
        Func<VariantHandlerContext, VariantHandlerContext> next
    )
    {
        var result = next(input);
        return result with { Selector = result.Selector.Class(PseudoClass) };
    }
}
