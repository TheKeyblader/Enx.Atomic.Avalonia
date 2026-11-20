using System.Text.RegularExpressions;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Base contract for a UnoCSS-style shortcut: a named token that expands to other utility tokens before rule
/// matching (<c>done</c> → <c>line-through opacity-45</c>). Shortcuts are checked in
/// <see cref="AtomicConfiguration{TTheme}.Shortcuts"/> order, before any <see cref="IRule"/>, so a shortcut and
/// a rule sharing a name resolve as the shortcut.
/// </summary>
/// <remarks>
/// <para>
/// This interface deliberately declares no members: the two supported shapes are <see cref="IStaticShortcut"/>
/// (exact name) and <see cref="IDynamicShortcut{TTheme}"/> (regex), mirroring <see cref="IStaticRule"/> and
/// <see cref="IDynamicRule{TTheme}"/>. It is <em>contravariant</em> so the theme-independent
/// <see cref="IStaticShortcut"/> can be stored in a <see cref="AtomicConfiguration{TTheme}.Shortcuts"/> list for
/// any theme: a static shortcut never reads <typeparamref name="TTheme"/>, so implementing
/// <c>IShortcut&lt;object&gt;</c> is enough and the conversion to <c>IShortcut&lt;TTheme&gt;</c> is a plain
/// reference conversion. That is what lets <see cref="Shortcut.Static"/> stay non-generic.
/// </para>
/// </remarks>
/// <typeparam name="TTheme">The theme type a dynamic shortcut may read — see <see cref="IDynamicShortcut{TTheme}"/>.</typeparam>
public interface IShortcut<in TTheme>
    where TTheme : class { }

/// <summary>
/// A shortcut matched by exact token name, with a fixed expansion. Theme-independent by construction — see
/// <see cref="IShortcut{TTheme}"/> for why that makes it an <c>IShortcut&lt;object&gt;</c>.
/// </summary>
public interface IStaticShortcut : IShortcut<object>
{
    /// <summary>The exact token this shortcut matches.</summary>
    string Name { get; }

    /// <summary>
    /// The utility tokens this shortcut expands to, already whitespace-split, trimmed and deduplicated in
    /// declaration order (see <see cref="Shortcut.Static"/>). Never empty for a shortcut that was built through
    /// the factory; an empty expansion is treated as "no match".
    /// </summary>
    IReadOnlyList<string> Expansion { get; }
}

/// <summary>
/// A shortcut matched by regular expression, computing its expansion from the match and the active theme —
/// the shortcut counterpart of <see cref="IDynamicRule{TTheme}"/>.
/// </summary>
/// <typeparam name="TTheme">The theme type the matcher may read values from.</typeparam>
public interface IDynamicShortcut<TTheme> : IShortcut<TTheme>
    where TTheme : class
{
    /// <summary>The pattern the base (variant-stripped) token must match.</summary>
    Regex Regex { get; }

    /// <summary>
    /// Computes the expansion for a successful match, or returns <see langword="null"/>/an empty list to treat
    /// the match as a miss so the next configured shortcut gets a chance (exactly like a dynamic rule returning
    /// no <see cref="StyleValue"/>). Returned strings are whitespace-split, trimmed and deduplicated.
    /// </summary>
    /// <param name="match">The successful regex match.</param>
    /// <param name="context">The resolution context, giving access to the theme and the generator.</param>
    IReadOnlyList<string>? Match(Match match, RuleContext<TTheme> context);
}
