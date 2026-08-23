using System.Linq.Expressions;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.Compact;
using Enx.Atomic.Avalonia.Internal;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// The core engine that turns raw utility-class tokens (e.g. <c>"hover:bg-red-500"</c>) into Avalonia styles.
/// A token is first stripped of variants (<see cref="MatchVariants(string, string?)"/>), the remaining base
/// token is matched against configured <see cref="Rule"/>s to produce style values, and those values are
/// re-wrapped by the matched variants into a final selector/setter pair (<see cref="StringifiedUtil"/>).
/// Results are cached per raw token since the same token is typically encountered many times across a project.
/// </summary>
public class AtomicGenerator<TTheme>
    where TTheme : class
{
    /// <summary>The rules, variants, extractors, emitters and theme this generator resolves tokens against.</summary>
    public AtomicConfiguration<TTheme> Configuration { get; }
    private readonly HashSet<IRule> _activatedRules = [];
    private readonly Dictionary<string, StringifiedUtil[]> _cache = [];

    /// <summary>Creates a generator for the given configuration, assigning each rule its declaration-order index.</summary>
    /// <exception cref="InvalidOperationException">A configured rule is neither <see cref="IStaticRule"/> nor <see cref="IDynamicRule{TTheme}"/>.</exception>
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

    /// <summary>
    /// Runs the configured <see cref="ISourceTransformer{TTheme}"/>s over <paramref name="code"/>, grouped and
    /// run <c>pre</c> → <c>default</c> → <c>post</c> (declaration order within a group), each seeing the
    /// previous one's output — ported from UnoCSS's <c>applyTransformers</c>. Always runs before extraction.
    /// </summary>
    /// <param name="code">The source text to transform.</param>
    /// <param name="id">Optional identifier passed to transformers that filter by source.</param>
    public string ApplyTransformers(string code, string? id = null)
    {
        foreach (
            var stage in (SourceTransformerEnforce[])
                [SourceTransformerEnforce.Pre, SourceTransformerEnforce.Default, SourceTransformerEnforce.Post]
        )
        {
            foreach (var transformer in Configuration.Transformers)
            {
                if (transformer.Enforce != stage)
                    continue;

                if (transformer.IdFilter is not null && (id is null || !transformer.IdFilter(id)))
                    continue;

                if (transformer.CodeFilter is not null && !transformer.CodeFilter(code, id))
                    continue;

                code = transformer.Transform(code, id, this);
            }
        }

        return code;
    }

    /// <summary>Runs the configured <see cref="Extractor"/>s over <paramref name="code"/> to pull out candidate utility tokens.</summary>
    /// <param name="code">The source text to scan.</param>
    /// <param name="id">Optional identifier passed to extractors that need to scope tokens to this source.</param>
    /// <param name="extracted">An existing set to add tokens to, useful for accumulating across multiple files. A new set is created if omitted.</param>
    /// <returns>The set of tokens extracted, including any pre-existing entries from <paramref name="extracted"/>.</returns>
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

    /// <summary>Builds the <see cref="RuleContext{TTheme}"/> passed to rules when resolving a token, from the result of variant matching.</summary>
    /// <param name="raw">The original, unstripped token.</param>
    /// <param name="applied">The result of matching variants against <paramref name="raw"/>.</param>
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

    /// <summary>
    /// Resolves a single raw utility token into its final Avalonia styles, running pre-processors, variant
    /// matching, rule resolution and stringification. Results are cached by raw token.
    /// </summary>
    /// <param name="raw">The raw token, e.g. <c>"hover:bg-red-500"</c>.</param>
    /// <returns>The styles produced by the token, or an empty array if no rule matched.</returns>
    public StringifiedUtil[] ParseToken(string raw)
    {
        var cacheKey = $"{raw}";

        if (_cache.TryGetValue(cacheKey, out var value))
            return value;

        var current = Configuration.PreProcessors.Aggregate(raw, (acc, p) => p.Process(acc) ?? acc);

        var variantResults = MatchVariants(raw, current);
        var result = variantResults.SelectMany(HandleVariantResult);

        return _cache[cacheKey] = [.. result];

        StringifiedUtil[] HandleVariantResult(VariantMatchedResult<TTheme> matched)
        {
            var context = MakeContext(raw, matched);

            var parsed = ParseUtil(context.Match, context);
            return [.. parsed.SelectMany(x => StringifyUtils(x, context))];
        }
    }

    /// <summary>Strips and records every configured variant that matches <paramref name="raw"/>, starting from a fresh match state.</summary>
    /// <param name="raw">The original, unstripped token.</param>
    /// <param name="current">The token to start matching from; defaults to <paramref name="raw"/> if omitted.</param>
    /// <returns>
    /// One result per branch of the match. Most tokens produce a single result; variants whose handler is
    /// ambiguous (matches multiple ways) fork into one result per interpretation.
    /// </returns>
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

    /// <summary>
    /// Repeatedly matches configured variants against <paramref name="result"/>'s current token, peeling one
    /// variant off per pass until none apply. Variants marked <c>MultiPass</c> may match more than once;
    /// a variant whose handler returns more than one match forks the result recursively (one branch per match),
    /// unless it is itself <c>MultiPass</c>, which is not supported for ambiguous matches.
    /// </summary>
    /// <param name="result">The current match state to continue from.</param>
    /// <param name="context">Shared context (theme, generator) available to variant matchers.</param>
    /// <exception cref="InvalidOperationException">A <c>MultiPass</c> variant produced more than one match.</exception>
    /// <exception cref="Exception">More than 500 variant handlers were accumulated, indicating a runaway/cyclic match.</exception>
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

    /// <summary>Matches variants on <paramref name="input"/> and resolves each resulting branch against the configured rules.</summary>
    private ParsedUtil[] ParseUtil(string input, RuleContext<TTheme> context)
    {
        var variantResults = MatchVariants(input);
        return [.. variantResults.Select(v => ParseUtil(v, context)).SelectMany(x => x)];
    }

    /// <summary>
    /// Resolves a variant-stripped token against the configured rules: a matching static rule wins outright,
    /// otherwise dynamic rules are tried in declaration order and the first one that yields styles is used.
    /// </summary>
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

    /// <summary>Marks <paramref name="rule"/> as activated and groups its style values by owner type into one <see cref="ParsedUtil"/> per group.</summary>
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

    /// <summary>Applies variant handlers to a resolved util and converts each resulting <see cref="Internal.UtilObject"/> into a <see cref="StringifiedUtil"/>.</summary>
    private StringifiedUtil[] StringifyUtils(ParsedUtil parsed, RuleContext<TTheme> context)
    {
        var utilities = ApplyVariants(parsed);
        List<StringifiedUtil> result = [];
        foreach (var util in utilities)
        {
            result.Add(
                new StringifiedUtil
                {
                    Selector = util.Selector,
                    SelectorData = util.SelectorData,
                    Body =
                    [
                        .. util.Entries.Select(x => new Setter(x.UntypedProperty, x.UntypedValue)),
                    ],
                    Index = parsed.Index,
                    Metadata = parsed.Metadata,
                    ContainerQuery = util.ContainerQuery,
                    ContainerQueryData = util.ContainerQueryData,
                }
            );
        }

        return [.. result];
    }

    /// <summary>
    /// Runs the parsed util's variant handlers, outermost-declared first, composing them into a single
    /// pipeline that starts from a base selector/entry set and folds in each handler's selector, container
    /// query and entry transformations. The final expressions are compiled to lambdas for the emitter.
    /// </summary>
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
                SelectorData = variantContextResult.Selector,
                ContainerQuery = containerQuery,
                ContainerQueryData = variantContextResult.ContainerQuery,
                Entries = variantContextResult.Entries,
                Sort = variantContextResult.Sort,
            },
        ];
    }

    /// <summary>Extracts utility tokens from <paramref name="input"/> and resolves them all into styles.</summary>
    /// <param name="input">The source text to scan.</param>
    /// <param name="options">Generation options, including the optional source identifier used during extraction.</param>
    public StringifiedUtil[] Generate(string input, Options options)
    {
        var transformed = ApplyTransformers(input, options.Id);
        var tokens = ApplyExtractors(transformed, options.Id);
        return Generate(tokens, options);
    }

    /// <summary>Resolves an already-extracted set of tokens into styles, skipping duplicates and tokens that match no rule.</summary>
    public StringifiedUtil[] Generate(ISet<string> tokens, Options options)
    {
        var matched = new HashSet<string>();
        List<StringifiedUtil> sheet = [];

        foreach (var token in tokens)
        {
            if (matched.Contains(token))
                continue;

            // Ghost properties (see SpecialProperties) never produce real output on their own — their
            // owner type isn't a real Avalonia control, so a style scoped to it could never match anything.
            // They stay visible from ParseToken (an ISourceTransformer like GhostPropertyCombiner needs to
            // see them to recognize and combine them into a real composite property); this is the actual
            // emission boundary where an uncombined one is finally dropped.
            var payload = ParseToken(token)
                .Where(u => u.Body.Length == 0 || u.Body[0].Property?.OwnerType != typeof(SpecialProperties))
                .ToArray();
            if (payload.Length == 0)
                continue;

            matched.Add(token);
            sheet.AddRange(payload);
        }

        return [.. sheet];
    }

    /// <summary>Options controlling a single <see cref="Generate(string, Options)"/> call.</summary>
    public class Options
    {
        /// <summary>Identifier scoping extracted tokens to a particular source, forwarded to extractors.</summary>
        public string? Id { get; init; }
    }
}
