namespace Enx.Atomic.Avalonia;

public class AtomicGenerator<TTheme>
    where TTheme : class
{
    public AtomicConfiguration<TTheme> Configuration { get; }
    private readonly HashSet<Rule<TTheme>> _activatedRules = [];
    private readonly Dictionary<string, StringifiedUtil<TTheme>[]> _cache = [];

    public AtomicGenerator(AtomicConfiguration<TTheme> configuration)
    {
        Configuration = configuration;
        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        var index = 0;
        foreach (var rule in Configuration.Rules)
        {
            rule.Metadata.Index = index++;
        }
    }

    public HashSet<string> ApplyExtractors(string code, string? id = null, HashSet<string>? extracted = null)
    {
        extracted ??= [];

        var context = new ExtractorContext
        {
            RawCode = code,
            Code = code,
            Id = id,
            Extracted = extracted
        };

        foreach (var extractor in Configuration.Extractors.OrderBy(x => x.Order))
            extractor.Extract(context);

        return extracted;
    }

    public StringifiedUtil<TTheme>[] ParseToken(string raw)
    {
        var cacheKey = $"{raw}";

        if (_cache.TryGetValue(cacheKey, out var value))
            return value;

        var current = Configuration.PreProcessors
            .Aggregate(raw, (acc, p) => p.Process(acc) ?? acc);

         
    }

    public ParsedUtil[] ParseUtil(string input, RuleContext<TTheme> context)
    {
        var staticRule = this.Configuration.Rules.OfType<Rule<TTheme>.Static>()
            .FirstOrDefault(s => s.Name == context.CurrentSelector);
        if (staticRule?.Style.Any() == true)
            return this.ResolveStylingResult(input, staticRule.Style, staticRule, context);

        foreach (var rule in this.Configuration.Rules.OfType<Rule<TTheme>.Dynamic>())
        {
            var matches = rule.Regex.Matches(context.CurrentSelector);
            if (matches.Count == 0) continue;

            var result = rule.Matcher(matches, context);
            if (!result.Any()) continue;

            return this.ResolveStylingResult(input, result, rule, context);
        }

        return [];
    }

    public ParsedUtil[] ResolveStylingResult(string raw, IEnumerable<StyleValue> styleValues, Rule<TTheme> rule, RuleContext<TTheme> context)
    {
        if (!styleValues.Any()) return [];

        _activatedRules.Add(rule);
        var valueByOwners = styleValues.GroupBy(x => x.UntypedProperty.OwnerType);

        var parsedUtils = valueByOwners.Select(g =>
        {
            return new ParsedUtil
            {
                Index = rule.Metadata.Index,
                Raw = raw,
                StyleEntries = [.. g],
                Metadata = rule.Metadata,
            };
        });

        return [.. parsedUtils];

    }

    public Parsed Generate(string input, Options options)
    {
        var tokens = ApplyExtractors(input, options.Id);
        return Generate(tokens, options);
    }

    public Result Generate(ISet<string> tokens, Options options)
    {
    }

    public class Options
    {
        public string? Id { get; init; }
    }

    public class Result
    {
    }
}