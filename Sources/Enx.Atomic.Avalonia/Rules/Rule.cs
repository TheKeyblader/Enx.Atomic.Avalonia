using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Enx.Atomic.Avalonia;

/// <summary>Base contract shared by all rules: something that resolves a utility token into style values and carries generation metadata.</summary>
public interface IRule
{
    /// <summary>Bookkeeping metadata shared across registrations of this rule.</summary>
    RuleMetadata Metadata { get; }
}

/// <summary>A rule matched by exact token name, e.g. <c>"flex"</c> or <c>"hidden"</c>, with a fixed set of style values.</summary>
public interface IStaticRule : IRule
{
    /// <summary>The exact token this rule matches.</summary>
    string Name { get; }

    /// <summary>The style values applied when <see cref="Name"/> matches.</summary>
    ImmutableArray<StyleValue> Styles { get; }
}

/// <summary>A rule matched by regular expression, e.g. <c>"bg-(?&lt;color&gt;.+)"</c>, that computes its style values from the match and theme.</summary>
public interface IDynamicRule<TTheme> : IRule
    where TTheme : class
{
    /// <summary>The pattern a token's current (variant-stripped) selector must match.</summary>
    Regex Regex { get; }

    /// <summary>Computes the style values for a successful match, or an empty array if the match should be treated as a miss.</summary>
    ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context);
}

/// <summary>
/// Optional companion to <see cref="IDynamicRule{TTheme}"/> for a rule that consolidates what UnoCSS keeps as
/// several separate rule entries into one regex — a directional family such as <c>m-*</c>/<c>mx-*</c>/<c>ml-*</c>
/// (see <see cref="RuleContext{TTheme}"/> and the "one class per family" convention in Preset.Mini).
///
/// <para>
/// In UnoCSS those directions are distinct entries with increasing indices, so <c>m-4 mx-2</c> lets the axis win
/// and <c>mx-2 ml-1</c> lets the side win regardless of source order. A consolidated rule has a single
/// declaration index in a configuration, so the relative order of its matches has to be reported here instead;
/// <see cref="StyleOrdering"/> then sorts them by it immediately after the rule index, before any other
/// tie-breaker. This is deliberately a separate channel from <see cref="StringifiedUtil.Sort"/> (a variant hint)
/// so the two can never overwrite each other. Rules that don't implement this interface rank every match
/// <c>0</c>.
/// </para>
/// </summary>
public interface IOrderRankedDynamicRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class
{
    /// <summary>
    /// The rank of a successful match relative to the rule's other matches, ascending — a higher rank is emitted
    /// later and therefore wins a cascade conflict between two of them. Only called after <see cref="Match"/>
    /// returned a non-empty result, and must be a pure function of the match (the uniform direction is <c>0</c>).
    /// </summary>
    /// <param name="match">The successful regex match the rule just resolved.</param>
    int GetOrderRank(Match match);
}

/// <summary>Computes style values for a <see cref="Rule.Dynamic{TTheme}"/> from its regex match and the current <see cref="RuleContext{TTheme}"/>.</summary>
public delegate ImmutableArray<StyleValue> DynamicMatcher<TTheme>(Match match, RuleContext<TTheme> context)
    where TTheme : class;

/// <summary>Base record for the two supported rule kinds: <see cref="Static"/> (exact name) and <see cref="Dynamic{TTheme}"/> (regex-driven).</summary>
public abstract record Rule : IRule
{
    /// <inheritdoc/>
    public RuleMetadata Metadata { get; init; } = new();

    /// <summary>A rule matched by exact token name with a fixed set of styles.</summary>
    public record Static(string Name, ImmutableArray<StyleValue> Styles) : Rule, IStaticRule;

    /// <summary>A rule matched by regular expression, computing its styles via <paramref name="Matcher"/>.</summary>
    public record Dynamic<TTheme>(Regex Regex, DynamicMatcher<TTheme> Matcher) : Rule, IDynamicRule<TTheme>
        where TTheme : class
    {
        /// <inheritdoc/>
        public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context) => Matcher(match, context);
    }
}
