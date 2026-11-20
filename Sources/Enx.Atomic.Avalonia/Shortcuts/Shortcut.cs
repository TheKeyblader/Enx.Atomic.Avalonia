using System.Text.RegularExpressions;
using Enx.Atomic.Avalonia.Shortcuts;

namespace Enx.Atomic.Avalonia;

/// <summary>Computes a dynamic shortcut's expansion from its regex match and the resolution context.</summary>
/// <typeparam name="TTheme">The theme type the matcher may read values from.</typeparam>
/// <returns>
/// The expansion as a single whitespace-separated string, or <see langword="null"/> to treat this match as a
/// miss and let the next shortcut try.
/// </returns>
public delegate string? ShortcutMatcher<TTheme>(Match match, RuleContext<TTheme> context)
    where TTheme : class;

/// <summary>Computes a dynamic shortcut's expansion as several strings, concatenated and split like a single one.</summary>
/// <typeparam name="TTheme">The theme type the matcher may read values from.</typeparam>
/// <returns>
/// The expansion strings, or <see langword="null"/>/empty to treat this match as a miss and let the next
/// shortcut try.
/// </returns>
public delegate IEnumerable<string>? ShortcutCollectionMatcher<TTheme>(Match match, RuleContext<TTheme> context)
    where TTheme : class;

/// <summary>
/// Factories for the two shortcut shapes — the shortcut counterpart of <see cref="Rule"/>:
/// <see cref="Static"/> (exact name, fixed expansion) and <see cref="Dynamic{TTheme}"/> (regex-driven).
/// </summary>
/// <remarks>
/// <see cref="Static"/> needs no type argument even though <see cref="AtomicConfiguration{TTheme}.Shortcuts"/>
/// is generic: <see cref="IStaticShortcut"/> is an <c>IShortcut&lt;object&gt;</c> and
/// <see cref="IShortcut{TTheme}"/> is contravariant, so a theme-independent shortcut fits every theme.
/// </remarks>
public static class Shortcut
{
    /// <summary>Creates a shortcut matched by exact name, expanding to a fixed, whitespace-separated token list.</summary>
    /// <param name="name">The exact token to match, e.g. <c>"done"</c>.</param>
    /// <param name="expansion">
    /// The utility tokens to expand to, e.g. <c>"line-through opacity-45 dark:text-gray-400"</c> — split on
    /// whitespace, trimmed and deduplicated (first occurrence kept).
    /// </param>
    public static IStaticShortcut Static(string name, string expansion) => new StaticShortcut(name, expansion);

    /// <summary>Creates a shortcut matched by regex whose expansion is computed as one whitespace-separated string.</summary>
    /// <param name="regex">The pattern the base token must match.</param>
    /// <param name="matcher">
    /// Computes the expansion from the match, e.g. <c>(match, _) =&gt; $"bg-{match.Groups[1].Value}-500 px-4"</c>.
    /// Return <see langword="null"/> to treat the match as a miss.
    /// </param>
    /// <typeparam name="TTheme">The theme type the matcher may read values from.</typeparam>
    public static IDynamicShortcut<TTheme> Dynamic<TTheme>(Regex regex, ShortcutMatcher<TTheme> matcher)
        where TTheme : class =>
        new DynamicShortcut<TTheme>(
            regex,
            (match, context) => matcher(match, context) is { } expansion ? [expansion] : null
        );

    /// <summary>Creates a shortcut matched by regex whose expansion is computed as several strings.</summary>
    /// <param name="regex">The pattern the base token must match.</param>
    /// <param name="matcher">
    /// Computes the expansion strings from the match. Return <see langword="null"/> or an empty sequence to
    /// treat the match as a miss.
    /// </param>
    /// <typeparam name="TTheme">The theme type the matcher may read values from.</typeparam>
    public static IDynamicShortcut<TTheme> Dynamic<TTheme>(Regex regex, ShortcutCollectionMatcher<TTheme> matcher)
        where TTheme : class => new DynamicShortcut<TTheme>(regex, matcher);
}
