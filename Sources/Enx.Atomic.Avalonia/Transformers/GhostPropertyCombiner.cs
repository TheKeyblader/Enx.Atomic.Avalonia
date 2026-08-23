using System.Linq.Expressions;
using Avalonia;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Combines ghost-property utilities (see <see cref="SpecialProperties"/>) found on the same source line into
/// one real composite value, registered via <see cref="AtomicGenerator{TTheme}.AddUtil"/> as an extra
/// <see cref="StringifiedUtil"/> with a <b>compound selector</b> requiring every contributing class (e.g.
/// <c>.Class("ml-1").Class("mr-2")</c>). Its <see cref="Transform"/> always returns <paramref name="code"/>
/// unchanged — the source text is only ever <em>read</em> here, and the compound selector only ever requires
/// classes the real source already has, so nothing needs to be rewritten for this to work: not the in-memory
/// text (this method's own return value is the "in-memory text", and it's a no-op), and — crucially — never
/// the actual file on disk, which this engine has no notion of writing to in the first place.
/// </summary>
public sealed class GhostPropertyCombiner<TTheme> : ISourceTransformer<TTheme>
    where TTheme : class
{
    public string Name => "ghost-property-combiner";
    public SourceTransformerEnforce Enforce => SourceTransformerEnforce.Post;
    public Func<string, bool>? IdFilter => null;
    public Func<string, string?, bool>? CodeFilter => null;

    public string Transform(string code, string? id, AtomicGenerator<TTheme> generator)
    {
        var seen = new HashSet<string>();

        foreach (var line in code.Split('\n'))
        {
            var candidates = SplitExtractor
                .SplitRegex()
                .Split(line)
                .Where(token => token.Length > 0)
                .Distinct()
                .ToArray();

            var ghostHits = new List<(string Token, AvaloniaProperty Ghost, object? Value)>();
            foreach (var token in candidates)
            {
                foreach (var util in generator.ParseToken(token))
                foreach (var setter in util.Body)
                {
                    if (setter.Property is not null && GhostProperties.Map.ContainsKey(setter.Property))
                        ghostHits.Add((token, setter.Property, setter.Value));
                }
            }

            if (ghostHits.Count < 1)
                continue;

            // A lone ghost token (no sibling on this line) is still a "group" of one here — otherwise a ghost
            // property could never be used on its own.
            foreach (var group in ghostHits.GroupBy(hit => GhostProperties.Map[hit.Ghost].Real))
            {
                var tokens = group
                    .Select(hit => hit.Token)
                    .Distinct()
                    .OrderBy(t => t, StringComparer.Ordinal)
                    .ToArray();

                var key = string.Join(' ', tokens);
                if (!seen.Add(key))
                    continue;

                var slots = new float[4];
                foreach (var hit in group)
                    slots[GhostProperties.Map[hit.Ghost].Slot] = Convert.ToSingle(hit.Value);

                var value = GhostProperties.Map[group.First().Ghost].Build(slots);

                SelectorExpression selectorData = SelectorsExpression.Is(null, value.TargetType);
                foreach (var token in tokens)
                    selectorData = selectorData.Class(token);

                var selectorParameter = Expression.Parameter(typeof(Selector), "selector");
                var selector = Expression.Lambda<Func<Selector, Selector>>(
                    selectorData.ToExpression(selectorParameter),
                    true,
                    selectorParameter
                );

                generator.AddUtil(
                    new StringifiedUtil
                    {
                        Index = int.MaxValue,
                        Selector = selector,
                        SelectorData = selectorData,
                        Body = [new Setter(value.UntypedProperty, value.UntypedValue)],
                        Metadata = new RuleMetadata(),
                    }
                );
            }
        }

        return code;
    }
}
