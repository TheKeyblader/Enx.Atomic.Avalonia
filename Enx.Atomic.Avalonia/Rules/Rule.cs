using System.Text.RegularExpressions;
using Dunet;

namespace Enx.Atomic.Avalonia;

public delegate IEnumerable<StyleValue> DynamicMatcher<TTheme>(MatchCollection matches);

[Union]
public abstract partial record Rule<TTheme>
    where TTheme : class
{
    public RuleMetadata Metadata { get; } = new();

    public partial record Static(string Name, IEnumerable<StyleValue> Style);

    public partial record Dynamic(Regex Regex, DynamicMatcher<TTheme> Matcher);
}