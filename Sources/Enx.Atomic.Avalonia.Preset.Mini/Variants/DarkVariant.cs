using Avalonia.Controls;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia.Preset.Mini.Variants;

/// <summary>
/// Matches the Tailwind/UnoCSS-style <c>dark:</c> prefix and folds a check against the current
/// <see cref="ThemeVariant"/> into the resolved style's selector, via <see cref="Selectors.PropertyEquals"/> on
/// <see cref="ThemeVariantScope.ActualThemeVariantProperty"/>. That property is inherited, so it resolves
/// correctly on any element regardless of which type actually hosts the <see cref="ThemeVariantScope"/>
/// (typically a <see cref="TopLevel"/>) above it in the tree. <see cref="ThemeVariantScope"/> is passed
/// explicitly as the property's declaring type: it's the exact same <see cref="Avalonia.AvaloniaProperty"/>
/// instance as the internal <c>ThemeVariant.ActualThemeVariantProperty</c> it was originally registered on —
/// <c>OwnerType</c> always reports that original, non-public owner, the same <c>AddOwner</c> pitfall documented
/// on <see cref="Enx.Atomic.Avalonia.StyleValue.TargetType"/>.
/// </summary>
public record DarkVariant<TTheme> : VariantBase<TTheme>
    where TTheme : class
{
    public DarkVariant() => MultiPass = true;

    public override VariantHandlerBase[] Match(string matcher, VariantContext<TTheme> context)
    {
        const string prefix = "dark:";
        if (!matcher.StartsWith(prefix, StringComparison.Ordinal))
            return [];

        return [new DarkVariantHandler { Matcher = matcher[prefix.Length..] }];
    }
}

/// <summary>Appends a check against the current <see cref="ThemeVariant"/> to the resolved style's selector.</summary>
public record DarkVariantHandler : VariantHandlerBase
{
    public override VariantHandlerContext Handle(
        VariantHandlerContext input,
        Func<VariantHandlerContext, VariantHandlerContext> next
    )
    {
        var result = next(input);
        return result with
        {
            Selector = result.Selector.PropertyEquals(
                ThemeVariantScope.ActualThemeVariantProperty,
                ThemeVariant.Dark,
                typeof(ThemeVariantScope)
            ),
        };
    }
}
