using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Dunet;

namespace Enx.Atomic.Avalonia;

public interface IRule
{
    RuleMetadata Metadata { get; }
}

public interface IStaticRule : IRule
{
    string Name { get; }
    ImmutableArray<StyleValue> Styles { get; }
}

public interface IDynamicRule<TTheme> : IRule
    where TTheme : class
{
    Regex Regex { get; }
    ImmutableArray<StyleValue> Match(Match matches, RuleContext<TTheme> context);
}

public delegate ImmutableArray<StyleValue> DynamicMatcher<TTheme>(Match match, RuleContext<TTheme> context)
    where TTheme : class;

public abstract record Rule : IRule
{
    public RuleMetadata Metadata { get; init; } = new();
    public record Static(string Name, ImmutableArray<StyleValue> Styles) : Rule, IStaticRule;
    public record Dynamic<TTheme>(Regex Regex, DynamicMatcher<TTheme> Matcher) : Rule, IDynamicRule<TTheme>
        where TTheme : class
    {
        public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
            => Matcher(match, context);
    }
}


