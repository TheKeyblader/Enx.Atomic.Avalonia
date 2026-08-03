using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Dunet;

namespace Enx.Atomic.Avalonia;

/// <summary>Base contract shared by all rules: something that resolves a utility token into style values and carries generation metadata.</summary>
public interface IRule
{
    /// <summary>Bookkeeping metadata (declaration index, cache hash) assigned by <see cref="AtomicGenerator{TTheme}"/>.</summary>
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
        public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
            => Matcher(match, context);
    }
}


