using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia;

public class AtomicGenerator<TTheme>
    where TTheme : class
{
    public AtomicConfiguration<TTheme> Configuration { get; }
    private readonly HashSet<IRule> _activatedRules = [];
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

        if (!Configuration.Rules.All(r => r is IStaticRule or IDynamicRule<TTheme>))
            throw new InvalidOperationException();
    }

    public HashSet<string> ApplyExtractors(
        string code,
        string? id = null,
        HashSet<string>? extracted = null
    )
    {
        extracted ??= [];

        var context = new ExtractorContext
        {
            RawCode = code,
            Code = code,
            Id = id,
            Extracted = extracted,
        };

        foreach (var extractor in Configuration.Extractors.OrderBy(x => x.Order))
            extractor.Extract(context);

        return extracted;
    }

    public RuleContext<TTheme> MakeContext(string raw, VariantMatchedResult<TTheme> applied)
    {
        return new RuleContext<TTheme>
        {
            RawSelector = raw,
            CurrentSelector = applied.Current,
            Theme = Configuration.Theme,
            Generator = this,
            Handlers = applied.Handlers,
            Match = applied,
        };
    }

    public StringifiedUtil<TTheme>[] ParseToken(string raw)
    {
        var cacheKey = $"{raw}";

        if (_cache.TryGetValue(cacheKey, out var value))
            return value;

        var current = Configuration.PreProcessors.Aggregate(raw, (acc, p) => p.Process(acc) ?? acc);

        var variantResults = MatchVariants(raw, current);
        var result = variantResults.SelectMany(HandleVariantResult);

        return _cache[cacheKey] = [.. result];

        StringifiedUtil<TTheme>[] HandleVariantResult(VariantMatchedResult<TTheme> matched)
        {
            var context = MakeContext(raw, matched);

            var parsed = ParseUtil(context.Match, context);
            return [.. parsed.SelectMany(x => StringifyUtils(x, context))];
        }
    }

    public VariantMatchedResult<TTheme>[] MatchVariants(string raw, string? current = null)
    {
        var context = new VariantContext<TTheme>
        {
            RawSelector = raw,
            Generator = this,
            Theme = Configuration.Theme,
        };

        var matched = new VariantMatchedResult<TTheme> { Raw = raw, Current = current ?? raw };

        return MatchVariants(matched, context);
    }

    public VariantMatchedResult<TTheme>[] MatchVariants(
        VariantMatchedResult<TTheme> result,
        VariantContext<TTheme> context
    )
    {
        var applied = true;
        var handlers = result.Handlers;
        var variants = result.Variants;
        while (applied)
        {
            applied = false;
            var processed = result.Current;
            foreach (var variant in Configuration.Variants)
            {
                if (!variant.MultiPass && result.Variants.Contains(variant))
                    continue;

                var handler = variant.Match(processed, context);
                if (handler.Length == 0)
                    continue;

                if (handler.Length == 1)
                {
                    result = result with
                    {
                        Current = handler[0].Matcher ?? processed,
                        Handlers = [handler[0], .. result.Handlers],
                        Variants = [variant, .. result.Variants],
                    };
                    applied = true;
                    break;
                }

                if (variant.MultiPass)
                    throw new InvalidOperationException();

                var subMatchings = handler.Select(h =>
                {
                    var _processed = h.Matcher ?? processed;
                    VariantHandlerBase[] _handlers = [h, .. handlers];
                    HashSet<VariantBase<TTheme>> _variants = [variant, .. variants];
                    return new VariantMatchedResult<TTheme>
                    {
                        Raw = result.Raw,
                        Current = _processed,
                        Handlers = _handlers,
                        Variants = _variants,
                    };
                });

                return [.. subMatchings.Select(c => MatchVariants(c, context)).SelectMany(x => x)];
            }
            if (!applied)
                break;

            if (handlers.Length > 500)
                throw new Exception();
        }

        return [result];
    }

    private ParsedUtil[] ParseUtil(string input, RuleContext<TTheme> context)
    {
        var variantResults = MatchVariants(input);
        return [.. variantResults.Select(v => ParseUtil(v, context)).SelectMany(x => x)];
    }

    private ParsedUtil[] ParseUtil(
        VariantMatchedResult<TTheme> matched,
        RuleContext<TTheme> context
    )
    {
        var raw = matched.Raw;
        var processed = matched.Current;
        var variantHandlers = matched.Handlers;

        var scopeContext = context with { Handlers = [.. variantHandlers] };

        var staticRule = this
            .Configuration.Rules.OfType<IStaticRule>()
            .FirstOrDefault(s => s.Name == processed);
        if (staticRule?.Styles.Any() == true)
            return ResolveStylingResult(raw, staticRule.Styles, staticRule, scopeContext);

        foreach (var rule in this.Configuration.Rules.OfType<IDynamicRule<TTheme>>())
        {
            var match = rule.Regex.Match(context.CurrentSelector);
            if (!match.Success)
                continue;

            var result = rule.Match(match, context);
            if (!result.Any())
                continue;

            return ResolveStylingResult(raw, result, rule, scopeContext);
        }

        return [];
    }

    private ParsedUtil[] ResolveStylingResult(
        string raw,
        IEnumerable<StyleValue> styleValues,
        IRule rule,
        RuleContext<TTheme> context
    )
    {
        if (!styleValues.Any())
            return [];

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
                VariantHandlers = [.. context.Handlers],
            };
        });

        return [.. parsedUtils];
    }

    private StringifiedUtil<TTheme>[] StringifyUtils(ParsedUtil parsed, RuleContext<TTheme> context)
    {
        var utilities = ApplyVariants(parsed);
        List<StringifiedUtil<TTheme>> result = [];
        foreach (var util in utilities)
        {
            result.Add(
                new StringifiedUtil<TTheme>
                {
                    Selector = util.Selector,
                    Body =
                    [
                        .. util.Entries.Select(x => new Setter(x.UntypedProperty, x.UntypedValue)),
                    ],
                    Context = context,
                    Index = parsed.Index,
                    Metadata = parsed.Metadata,
                    ContainerQuery = util.ContainerQuery,
                }
            );
        }

        return [.. result];
    }

    private UtilObject[] ApplyVariants(
        ParsedUtil parsed,
        VariantHandlerBase[]? variantHanders = null,
        string? raw = null
    )
    {
        variantHanders ??= parsed.VariantHandlers;
        raw ??= parsed.Raw;

        var handler = variantHanders
            .OrderBy(x => x.Order)
            .Aggregate<VariantHandlerBase, Func<VariantHandlerContext, VariantHandlerContext>>(
                x => x,
                (previous, v) =>
                    (input) =>
                    {
                        var entries = input.Entries;
                        return v.Handle(input with { Entries = entries }, previous);
                    }
            );

        var variantContextResult = handler(
            new VariantHandlerContext
            {
                Selector = SelectorsExpression
                    .Is(null, parsed.StyleEntries[0].UntypedProperty.OwnerType)
                    .Class(parsed.Raw),
                ContainerQuery = null,
                Entries = parsed.StyleEntries,
            }
        );

        var selectorParameter = Expression.Parameter(typeof(Selector), "selector");
        var selector = Expression.Lambda<Func<Selector, Selector>>(
            variantContextResult.Selector.ToExpression(selectorParameter),
            true,
            selectorParameter
        );

        var containerQueryParameter = Expression.Parameter(typeof(StyleQuery), "query");
        var containerQuery =
            variantContextResult.ContainerQuery == null
                ? null
                : Expression.Lambda<Func<StyleQuery, StyleQuery>>(
                    variantContextResult.ContainerQuery.ToExpression(containerQueryParameter),
                    true,
                    containerQueryParameter
                );

        return
        [
            new UtilObject
            {
                Selector = selector,
                ContainerQuery = containerQuery,
                Entries = variantContextResult.Entries,
                Sort = variantContextResult.Sort,
            },
        ];
    }

    public StringifiedUtil<TTheme>[] Generate(string input, Options options)
    {
        var tokens = ApplyExtractors(input, options.Id);
        return Generate(tokens, options);
    }

    public StringifiedUtil<TTheme>[] Generate(ISet<string> tokens, Options options)
    {
        var matched = new HashSet<string>();
        List<StringifiedUtil<TTheme>> sheet = [];

        foreach (var token in tokens)
        {
            if (matched.Contains(token))
                continue;

            var payload = ParseToken(token);
            if (payload.Length == 0)
                continue;

            matched.Add(token);
            sheet.AddRange(payload);
        }

        return [.. sheet];
    }

    public class Options
    {
        public string? Id { get; init; }
    }

    public class Result
    {
        public string Content { get; init; }
    }
}
