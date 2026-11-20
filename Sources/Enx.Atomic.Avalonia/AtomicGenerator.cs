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
    /// <summary>
    /// How many nested shortcuts an expansion may enter before it is treated as a cycle. UnoCSS's
    /// <c>expandShortcut(..., depth = 5)</c> equivalent; a few levels cover realistic composition, and a
    /// self-referencing shortcut must fail loudly rather than recurse forever.
    /// </summary>
    private const int MaxShortcutDepth = 5;

    /// <summary>The rules, variants, extractors, emitters and theme this generator resolves tokens against.</summary>
    public AtomicConfiguration<TTheme> Configuration { get; }
    private readonly HashSet<IRule> _activatedRules = [];
    private readonly Dictionary<string, StringifiedUtil[]> _cache = [];
    private readonly Dictionary<string, string[]> _shortcutExpansions = [];
    private readonly List<string> _diagnostics = [];
    private readonly HashSet<string> _diagnosticSet = [];
    private readonly List<StringifiedUtil> _pendingUtils = [];
    private readonly Dictionary<string, StyleValue.Resource> _resolvedResources = [];

    /// <summary>
    /// Every distinct <see cref="StyleValue.Resource"/> encountered so far, keyed by <see cref="StyleValue.Resource.Key"/>
    /// — the data <c>ResourceDictionaryEmitter</c> (in <c>Enx.Atomic.Avalonia.CodeGen</c>) needs to build the
    /// actual resource dictionary a generated <c>Styles</c> class's <c>DynamicResource</c> lookups resolve
    /// against. Deduplicated by key: several rules (or several <c>StyleValue</c>s from the same rule, e.g.
    /// <c>bg-*</c>'s <c>Border</c>/<c>TemplatedControl</c>/<c>Panel</c> triple) commonly share one resource.
    /// </summary>
    public IReadOnlyDictionary<string, StyleValue.Resource> ResolvedResources => _resolvedResources;

    /// <summary>
    /// Non-fatal problems found while resolving shortcuts — today only an expansion token no rule or shortcut
    /// matched (a configuration typo). Deduplicated in first-seen order, so a token used across several sources
    /// reports once. A build must not fail on these: the rest of the shortcut still applies, so <c>AtomicCli</c>
    /// prints them as warnings rather than throwing.
    /// </summary>
    public IReadOnlyList<string> Diagnostics => _diagnostics;

    /// <summary>Creates a generator for the given configuration, assigning each rule its declaration-order index.</summary>
    /// <exception cref="InvalidOperationException">
    /// A configured rule is neither <see cref="IStaticRule"/> nor <see cref="IDynamicRule{TTheme}"/>, or a
    /// configured shortcut is neither <see cref="IStaticShortcut"/> nor <see cref="IDynamicShortcut{TTheme}"/>.
    /// </exception>
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
        {
            var offending = Configuration.Rules.First(r => r is not (IStaticRule or IDynamicRule<TTheme>));
            throw new InvalidOperationException(
                $"Configured rule '{offending.GetType().FullName}' is neither an IStaticRule nor an IDynamicRule<{typeof(TTheme).Name}>; every rule passed to AtomicConfiguration<{typeof(TTheme).Name}>.Rules must implement one of them."
            );
        }

        if (!Configuration.Shortcuts.All(s => s is IStaticShortcut or IDynamicShortcut<TTheme>))
        {
            var offending = Configuration.Shortcuts.First(s => s is not (IStaticShortcut or IDynamicShortcut<TTheme>));
            throw new InvalidOperationException(
                $"Configured shortcut '{offending.GetType().FullName}' is neither an IStaticShortcut nor an IDynamicShortcut<{typeof(TTheme).Name}>; every shortcut passed to AtomicConfiguration<{typeof(TTheme).Name}>.Shortcuts must implement one of them."
            );
        }
    }

    /// <summary>
    /// Runs the configured <see cref="ISourceTransformer{TTheme}"/>s over <paramref name="code"/>, grouped and
    /// run <c>pre</c> → <c>default</c> → <c>post</c> (declaration order within a group), each seeing the
    /// previous one's output — ported from UnoCSS's <c>applyTransformers</c>. Always runs before extraction.
    /// Clears and repopulates <see cref="_pendingUtils"/> — a transformer that needs to emit a style whose
    /// selector isn't derived from a single matched token (e.g. one spanning several classes at once) calls
    /// <see cref="AddUtil"/> as a side effect, rather than trying to express that through its returned text.
    /// </summary>
    /// <param name="code">The source text to transform.</param>
    /// <param name="id">Optional identifier passed to transformers that filter by source.</param>
    public string ApplyTransformers(string code, string? id = null)
    {
        _pendingUtils.Clear();

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

    /// <summary>
    /// Registers <paramref name="util"/> to be included in the next <see cref="Generate(string, Options)"/>
    /// call's result, bypassing normal token-to-selector resolution. For a <see cref="ISourceTransformer{TTheme}"/>
    /// whose style doesn't correspond to any single matched token — e.g. a compound selector spanning several
    /// classes at once, built directly rather than from <c>Is(OwnerType).Class(rawToken)</c>.
    /// </summary>
    public void AddUtil(StringifiedUtil util) => _pendingUtils.Add(util);

    /// <summary>
    /// The utilities a <see cref="ISourceTransformer{TTheme}"/> registered through <see cref="AddUtil"/> during
    /// the most recent <see cref="ApplyTransformers"/> call. <see cref="Generate(string, Options)"/> folds them
    /// in automatically; a caller orchestrating several sources itself (e.g. <c>AtomicCli</c>) reads this after
    /// each file so the per-file transformer output can be ordered and deduplicated with the rest.
    /// </summary>
    public IReadOnlyList<StringifiedUtil> PendingUtils => _pendingUtils;

    /// <summary>Runs the configured <see cref="Extractor"/>s over <paramref name="code"/> to pull out candidate utility tokens.</summary>
    /// <param name="code">The source text to scan.</param>
    /// <param name="id">Optional identifier passed to extractors that need to scope tokens to this source.</param>
    /// <param name="extracted">An existing set to add tokens to, useful for accumulating across multiple files. A new set is created if omitted.</param>
    /// <returns>The set of tokens extracted, including any pre-existing entries from <paramref name="extracted"/>.</returns>
    public HashSet<string> ApplyExtractors(string code, string? id = null, HashSet<string>? extracted = null)
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
    /// matching, shortcut expansion, rule resolution and stringification. Results are cached by raw token.
    /// </summary>
    /// <remarks>
    /// Shortcuts are tried before rules (see <see cref="IShortcut{TTheme}"/>): a base token that matches one is
    /// expanded into further tokens, each resolved through the normal pipeline and re-keyed on the raw token the
    /// user wrote, so the outer class (<c>hover:btn-red</c>) is what the final selectors target. The inner tokens
    /// are never cached or emitted as standalone utilities.
    /// </remarks>
    /// <param name="raw">The raw token, e.g. <c>"hover:bg-red-500"</c>.</param>
    /// <returns>The styles produced by the token, or an empty array if no rule matched.</returns>
    public StringifiedUtil[] ParseToken(string raw)
    {
        if (_cache.TryGetValue(raw, out var value))
            return value;

        var current = Configuration.PreProcessors.Aggregate(raw, (acc, p) => p.Process(acc) ?? acc);

        var variantResults = MatchVariants(raw, current);
        var result = variantResults.SelectMany(HandleVariantResult);

        return _cache[raw] = [.. result];

        StringifiedUtil[] HandleVariantResult(VariantMatchedResult<TTheme> matched)
        {
            var context = MakeContext(raw, matched);

            var expansion = ExpandShortcut(context.CurrentSelector, context);
            if (expansion is not null)
                return ExpandShortcutToken(raw, matched, expansion);

            var parsed = ParseUtil(context.Match, context);
            return [.. parsed.SelectMany(x => StringifyUtils(x, StyleLayer.Default))];
        }
    }

    /// <summary>
    /// The leaf utility tokens the most recent <see cref="ParseToken"/> resolved for a shortcut token, e.g.
    /// <c>btn-red</c> → <c>bg-red-500</c>, <c>px-4</c>; <see langword="null"/> when that token's base is not a
    /// shortcut (or it wasn't parsed yet). Diagnostic only — mirrors <c>probe</c>'s need to show the expansion
    /// without re-deriving it, and deliberately returns tokens rather than styles since the styles are already in
    /// the parse result.
    /// </summary>
    /// <param name="raw">The raw token whose expansion to read.</param>
    public IReadOnlyList<string>? GetShortcutExpansion(string raw)
    {
        _ = ParseToken(raw);
        return _shortcutExpansions.TryGetValue(raw, out var expansion) ? expansion : null;
    }

    /// <summary>
    /// Expands <paramref name="token"/> against the configured shortcuts, in declaration order: the first static
    /// exact-name match or first dynamic regex match yielding a non-empty expansion wins.
    /// </summary>
    /// <param name="token">The variant-stripped base token, e.g. <c>btn-red</c>.</param>
    /// <param name="context">The resolution context handed to a dynamic shortcut's matcher (for the theme).</param>
    /// <returns>The expansion tokens, or <see langword="null"/> when no shortcut matched.</returns>
    private IReadOnlyList<string>? ExpandShortcut(string token, RuleContext<TTheme> context)
    {
        foreach (var shortcut in Configuration.Shortcuts)
        {
            switch (shortcut)
            {
                case IStaticShortcut { Expansion.Count: > 0 } staticShortcut when staticShortcut.Name == token:
                    return staticShortcut.Expansion;

                case IDynamicShortcut<TTheme> dynamicShortcut:
                {
                    var match = dynamicShortcut.Regex.Match(token);
                    if (!match.Success)
                        continue;

                    var expansion = dynamicShortcut.Match(match, context);
                    if (expansion is { Count: > 0 })
                        return expansion;

                    // A dynamic shortcut whose matcher returned nothing is a miss: keep looking, exactly like a
                    // dynamic rule that matches but yields no StyleValue.
                    break;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves a shortcut expansion into styles: one <see cref="StringifiedUtil"/> per inner utility (per
    /// target-type group, as for a plain token), all selecting on the outer raw token and carrying the
    /// <see cref="StyleLayer.Shortcuts"/> layer.
    /// </summary>
    /// <remarks>
    /// Deliberately no merging of setters into one style: keeping each inner utility separate preserves its own
    /// rule index/rank (so <c>p-2 px-4</c> inside a shortcut keeps uniform-before-axis on the composite side
    /// properties) and avoids two setters for the same property in one <c>Style</c>. An inner token nothing
    /// matches is a configuration typo: it is recorded in <see cref="Diagnostics"/> and skipped, never thrown, so
    /// the rest of the shortcut still applies.
    /// </remarks>
    private StringifiedUtil[] ExpandShortcutToken(
        string raw,
        VariantMatchedResult<TTheme> matched,
        IReadOnlyList<string> expansion
    )
    {
        List<(string Token, VariantMatchedResult<TTheme> Matched, RuleContext<TTheme> Context)> leaves = [];
        CollectShortcutLeaves(expansion, matched.Handlers, raw, prefix: "", depth: 0, [raw], leaves);

        var uniqueLeaves = leaves.DistinctBy(leaf => leaf.Token, StringComparer.Ordinal).ToArray();
        _shortcutExpansions[raw] = [.. uniqueLeaves.Select(leaf => leaf.Token)];

        List<StringifiedUtil> result = [];
        foreach (var leaf in uniqueLeaves)
        {
            var parsed = ParseUtil(leaf.Matched, leaf.Context);
            if (parsed.Length == 0)
            {
                AddDiagnostic($"unmatched utility \"{leaf.Token}\" in shortcut \"{raw}\"");
                continue;
            }

            result.AddRange(parsed.SelectMany(x => StringifyUtils(x, StyleLayer.Shortcuts)));
        }

        return [.. result];
    }

    /// <summary>
    /// Walks an expansion, resolving nested shortcuts recursively while combining each level's variant handlers
    /// with the parent's (inner handlers first, then outer), so <c>hover:nested</c> inside a shortcut works. Each
    /// leaf keeps the outer raw token as its base class and the merged handlers as its
    /// <see cref="ParsedUtil.VariantHandlers"/>; <see cref="ApplyVariants"/> then sorts the composed pipeline by
    /// handler order.
    /// </summary>
    /// <param name="expansion">The tokens to resolve at this level.</param>
    /// <param name="outerHandlers">The variant handlers accumulated by the enclosing tokens.</param>
    /// <param name="raw">The raw token the user wrote — the base class every resulting style selects on.</param>
    /// <param name="prefix">The variant text the enclosing levels contributed (<c>hover:</c> for <c>hover:inner</c>), prepended to each leaf's written form.</param>
    /// <param name="depth">How many nested shortcuts were entered so far.</param>
    /// <param name="chain">The shortcut tokens entered so far, for the cycle error message.</param>
    /// <param name="leaves">The leaf tokens (as written, variants included) to resolve through rules, with their merged match/context.</param>
    private void CollectShortcutLeaves(
        IReadOnlyList<string> expansion,
        VariantHandlerBase[] outerHandlers,
        string raw,
        string prefix,
        int depth,
        List<string> chain,
        List<(string Token, VariantMatchedResult<TTheme> Matched, RuleContext<TTheme> Context)> leaves
    )
    {
        foreach (var token in expansion)
        {
            // A shortcut token may itself carry variants (hover:inner, sm:...), which must wrap the outer ones.
            foreach (var innerMatched in MatchVariants(token))
            {
                var handlers = new VariantHandlerBase[innerMatched.Handlers.Length + outerHandlers.Length];
                innerMatched.Handlers.CopyTo(handlers, 0);
                outerHandlers.CopyTo(handlers, innerMatched.Handlers.Length);

                var merged = innerMatched with { Raw = raw, Handlers = handlers };
                var context = MakeContext(raw, merged);

                // The leaf's identity is the token as written with its variants (hover:opacity-50), not the bare
                // base token, so two leaves differing only by variant both survive deduplication — UnoCSS's
                // uniq over the rewritten strings. The variant prefix is whatever precedes the base token.
                var matcherIndex = token.LastIndexOf(merged.Current, StringComparison.Ordinal);
                var variantPrefix = matcherIndex < 0 ? "" : token[..matcherIndex];
                var written = prefix + (matcherIndex < 0 ? token : variantPrefix + merged.Current);

                var nested = ExpandShortcut(merged.Current, context);
                if (nested is null)
                {
                    leaves.Add((written, merged, context));
                    continue;
                }

                if (depth >= MaxShortcutDepth)
                    throw new InvalidOperationException(
                        $"Shortcut expansion exceeded {MaxShortcutDepth} nested shortcuts while resolving '{raw}': "
                            + $"{string.Join(" -> ", chain.Append(merged.Current))}. A shortcut probably references itself, directly or through another shortcut."
                    );

                chain.Add(merged.Current);
                CollectShortcutLeaves(nested, handlers, raw, prefix + variantPrefix, depth + 1, chain, leaves);
                chain.RemoveAt(chain.Count - 1);
            }
        }
    }

    /// <summary>Records a non-fatal resolution problem once, preserving first-seen order — see <see cref="Diagnostics"/>.</summary>
    private void AddDiagnostic(string message)
    {
        if (_diagnosticSet.Add(message))
            _diagnostics.Add(message);
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
    /// <exception cref="InvalidOperationException">
    /// A <c>MultiPass</c> variant produced more than one match, or more than 500 variant handlers were
    /// accumulated (indicating a runaway/cyclic match).
    /// </exception>
    public VariantMatchedResult<TTheme>[] MatchVariants(
        VariantMatchedResult<TTheme> result,
        VariantContext<TTheme> context
    )
    {
        var applied = true;
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
                    throw new InvalidOperationException(
                        $"MultiPass variant '{variant.GetType().FullName}' matched token '{processed}' ambiguously ({handler.Length} handlers). A MultiPass variant may match more than once, but each match must be unambiguous; make its Match return a single handler per pass, or drop MultiPass."
                    );

                // Each branch continues from everything matched so far (this pass's earlier variants included),
                // not from the state the call started with.
                var current = result;
                var subMatchings = handler.Select(h => new VariantMatchedResult<TTheme>
                {
                    Raw = current.Raw,
                    Current = h.Matcher ?? processed,
                    Handlers = [h, .. current.Handlers],
                    Variants = [variant, .. current.Variants],
                });

                return [.. subMatchings.Select(c => MatchVariants(c, context)).SelectMany(x => x)];
            }
            if (!applied)
                break;

            if (result.Handlers.Length > 500)
                throw new InvalidOperationException(
                    $"More than 500 variant handlers were accumulated while resolving token '{result.Raw}' (current: '{result.Current}'). This usually means a variant keeps re-matching its own output, i.e. a cyclic variant; check MultiPass variants that rewrite the token into a form they still accept."
                );
        }

        return [result];
    }

    /// <summary>
    /// Resolves a variant-stripped token against the configured rules: a matching static rule wins outright,
    /// otherwise dynamic rules are tried in declaration order and the first one that yields styles is used.
    /// </summary>
    private ParsedUtil[] ParseUtil(VariantMatchedResult<TTheme> matched, RuleContext<TTheme> context)
    {
        var raw = matched.Raw;
        var processed = matched.Current;
        var variantHandlers = matched.Handlers;

        var scopeContext = context with { Handlers = [.. variantHandlers] };

        var staticRule = this.Configuration.Rules.OfType<IStaticRule>().FirstOrDefault(s => s.Name == processed);
        if (staticRule?.Styles.Any() == true)
            return ResolveStylingResult(raw, staticRule.Styles, staticRule, scopeContext, rank: 0);

        foreach (var rule in this.Configuration.Rules.OfType<IDynamicRule<TTheme>>())
        {
            var match = rule.Regex.Match(context.CurrentSelector);
            if (!match.Success)
                continue;

            var result = rule.Match(match, context);
            if (!result.Any())
                continue;

            var rank = rule is IOrderRankedDynamicRule<TTheme> rankedRule ? rankedRule.GetOrderRank(match) : 0;
            return ResolveStylingResult(raw, result, rule, scopeContext, rank);
        }

        return [];
    }

    /// <summary>Marks <paramref name="rule"/> as activated and groups its style values by owner type into one <see cref="ParsedUtil"/> per group.</summary>
    private ParsedUtil[] ResolveStylingResult(
        string raw,
        IEnumerable<StyleValue> styleValues,
        IRule rule,
        RuleContext<TTheme> context,
        int rank
    )
    {
        if (!styleValues.Any())
            return [];

        _activatedRules.Add(rule);
        foreach (var resource in styleValues.OfType<StyleValue.Resource>())
            _resolvedResources.TryAdd(resource.Key, resource);

        var valueByOwners = styleValues.GroupBy(x => x.TargetType);

        var parsedUtils = valueByOwners.Select(g =>
        {
            return new ParsedUtil
            {
                Index = rule.Metadata.Index,
                Rank = rank,
                Raw = raw,
                StyleEntries = [.. g],
                Metadata = rule.Metadata,
                VariantHandlers = [.. context.Handlers],
            };
        });

        return [.. parsedUtils];
    }

    /// <summary>Applies variant handlers to a resolved util and converts each resulting <see cref="Internal.UtilObject"/> into a <see cref="StringifiedUtil"/>.</summary>
    private StringifiedUtil[] StringifyUtils(ParsedUtil parsed, int layer)
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
                    Body = [.. util.Entries.Select(x => new Setter(x.UntypedProperty, x.UntypedValue))],
                    Index = parsed.Index,
                    Rank = parsed.Rank,
                    Layer = layer,
                    Metadata = parsed.Metadata,
                    ContainerQuery = util.ContainerQuery,
                    ContainerQueryData = util.ContainerQueryData,
                    Sort = util.Sort,
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
                Selector = SelectorsExpression.Is(null, parsed.StyleEntries[0].TargetType).Class(parsed.Raw),
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
    /// <remarks>
    /// Utilities are returned in <see cref="StyleOrdering"/>'s canonical emission order — Avalonia resolves equal
    /// priority frames by insertion order, so the order a <c>Styles</c> collection is populated in <em>is</em> the
    /// cascade (see <see cref="StyleOrdering"/>).
    /// </remarks>
    public StringifiedUtil[] Generate(string input, Options options)
    {
        var transformed = ApplyTransformers(input, options.Id);
        var tokens = ApplyExtractors(transformed, options.Id);

        // Sort once more over the concatenation: utils a transformer registered through AddUtil are only known
        // after ApplyTransformers ran, and must be ordered together with the rest.
        return StyleOrdering.Sort([.. Generate(tokens, options), .. _pendingUtils]);
    }

    /// <summary>Resolves an already-extracted set of tokens into styles, skipping duplicates and tokens that match no rule.</summary>
    /// <remarks>Utilities are returned in <see cref="StyleOrdering"/>'s canonical emission order.</remarks>
    public StringifiedUtil[] Generate(ISet<string> tokens, Options options)
    {
        var matched = new HashSet<string>();
        List<StringifiedUtil> sheet = [];

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

        return StyleOrdering.Sort(sheet);
    }

    /// <summary>Options controlling a single <see cref="Generate(string, Options)"/> call.</summary>
    public class Options
    {
        /// <summary>Identifier scoping extracted tokens to a particular source, forwarded to extractors.</summary>
        public string? Id { get; init; }
    }
}
