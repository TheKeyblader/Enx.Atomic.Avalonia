using Avalonia;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Combines ghost-property utilities (see <see cref="SpecialProperties"/>) found co-occurring on the same
/// source line into one real composite value, registered as a new static rule and appended to the line as a
/// synthetic token — alongside, not instead of, the original tokens, so a ghost-property utility used alone
/// still falls back to its own (zero-elsewhere) style. Runs <see cref="SourceTransformerEnforce.Post"/>, over
/// the whole line rather than one token at a time, since combining requires seeing several tokens together.
/// Same-line co-occurrence is a deliberately cheap heuristic (no XAML/AST awareness) — good enough for the
/// common <c>Classes="ml-1 mr-2"</c> case.
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
        var lines = code.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var candidates = SplitExtractor
                .SplitRegex()
                .Split(lines[i])
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

            // Groups of 1 are the common "used alone" case (e.g. a lone ml-4): still synthesized through the
            // same path, so a ghost property never needs a separate real-property fallback rule of its own.
            foreach (var group in ghostHits.GroupBy(hit => GhostProperties.Map[hit.Ghost].Real))
            {
                var slots = new float[4];
                foreach (var hit in group)
                    slots[GhostProperties.Map[hit.Ghost].Slot] = Convert.ToSingle(hit.Value);

                var value = GhostProperties.Map[group.First().Ghost].Build(slots);
                var syntheticName = $"__ghost_{string.Join('_', group.Select(hit => hit.Token).Distinct().OrderBy(t => t))}__";

                if (generator.Configuration.Rules.OfType<IStaticRule>().All(r => r.Name != syntheticName))
                {
                    var rule = new Rule.Static(syntheticName, [value]);
                    rule.Metadata.Index = generator.Configuration.Rules.Count;
                    generator.Configuration.Rules.Add(rule);
                }

                lines[i] = $"{lines[i]} {syntheticName}";
            }
        }

        return string.Join('\n', lines);
    }
}
