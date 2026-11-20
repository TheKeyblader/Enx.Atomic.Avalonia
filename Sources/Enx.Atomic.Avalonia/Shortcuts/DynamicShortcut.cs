using System.Text.RegularExpressions;

namespace Enx.Atomic.Avalonia.Shortcuts;

/// <summary>
/// The <see cref="IDynamicShortcut{TTheme}"/> implementation both <see cref="Shortcut.Dynamic{TTheme}"/>
/// overloads return. Kept internal — the factory's interface return type is the extension point, not the
/// concrete record.
/// </summary>
/// <param name="Regex">The pattern the base token must match.</param>
/// <param name="Matcher">Returns the raw expansion strings, or <see langword="null"/> for a miss.</param>
internal sealed record DynamicShortcut<TTheme>(Regex Regex, ShortcutCollectionMatcher<TTheme> Matcher)
    : IDynamicShortcut<TTheme>
    where TTheme : class
{
    /// <inheritdoc/>
    public IReadOnlyList<string>? Match(Match match, RuleContext<TTheme> context)
    {
        var expansions = Matcher(match, context);
        if (expansions is null)
            return null;

        var tokens = ShortcutExpansion.Split(expansions);
        return tokens.Length == 0 ? null : tokens;
    }
}
