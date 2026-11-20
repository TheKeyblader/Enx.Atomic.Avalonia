using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// Resolves the tokens a <c>probe</c> invocation extracted and records everything an agent or human needs to
/// understand the outcome: variant matching, the winning rule, every resolved style in final emission order with
/// its ordering key, the resources involved, and — with <c>--control</c> — the values Avalonia actually applies.
/// Deliberately works off the same public API the runtime and build-time paths share
/// (<see cref="AtomicGenerator{TTheme}.ParseToken"/>, <see cref="StyleOrdering.Sort"/>) instead of re-deriving
/// any of it, so the probe can't disagree with what gets emitted.
/// </summary>
internal static class ProbeRunner<TTheme>
    where TTheme : class
{
    /// <summary>Builds the probe report for <paramref name="settings"/> against <paramref name="configuration"/>.</summary>
    public static ProbeReport Run(ProbeSettings settings, AtomicConfiguration<TTheme> configuration)
    {
        var generator = new AtomicGenerator<TTheme>(configuration);
        var tokens = Extract(generator, settings);

        // Resolve every token once (mirroring generate's single shared token set), then order the union. The
        // position of each style is its index in that final order — the cascade, not the extraction order.
        var utilsByToken = new Dictionary<string, StringifiedUtil[]>(StringComparer.Ordinal);
        var all = new List<StringifiedUtil>();
        foreach (var token in tokens)
        {
            var utils = generator.ParseToken(token);
            utilsByToken[token] = utils;
            all.AddRange(utils);
        }

        var ordered = StyleOrdering.Sort(all);
        var positions = new Dictionary<StringifiedUtil, int>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < ordered.Length; i++)
            positions[ordered[i]] = i + 1;

        var resourceInfos = ResolveResources(generator, ordered);

        ProbeControlReport? control = null;
        if (!string.IsNullOrWhiteSpace(settings.Control))
        {
            control = ProbeControlRunner.Run(
                settings.Control,
                settings.Width,
                settings.PseudoClasses,
                configuration.ContainerName,
                tokens,
                ordered,
                resourceInfos
            );
        }

        return new ProbeReport
        {
            Tokens =
            [
                .. tokens.Select(token =>
                    DescribeToken(generator, configuration, token, utilsByToken[token], positions, ordered.Length)
                ),
            ],
            Resources =
            [
                .. resourceInfos.Select(info => new ProbeResourceReport { Key = info.Key, Themed = info.Themed }),
            ],
            Control = control,
        };
    }

    /// <summary>Runs the configured extractors over the argument text and any <c>--file</c> sources, keeping first-seen order.</summary>
    private static List<string> Extract(AtomicGenerator<TTheme> generator, ProbeSettings settings)
    {
        var tokens = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(HashSet<string> extracted)
        {
            foreach (var token in extracted)
            {
                if (!string.IsNullOrWhiteSpace(token) && seen.Add(token))
                    tokens.Add(token);
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.Text))
            Add(generator.ApplyExtractors(settings.Text));

        foreach (var file in settings.Files)
        {
            if (!File.Exists(file))
                continue;

            Add(generator.ApplyExtractors(File.ReadAllText(file), file));
        }

        return tokens;
    }

    private static ProbeTokenReport DescribeToken(
        AtomicGenerator<TTheme> generator,
        AtomicConfiguration<TTheme> configuration,
        string token,
        StringifiedUtil[] utils,
        Dictionary<StringifiedUtil, int> positions,
        int positionCount
    )
    {
        var match = generator.MatchVariants(token)[0];
        var variants = DescribeVariants(token, match, configuration);
        var hints = utils.Length == 0 ? BuildHints(generator, configuration, token, match, variants) : [];

        var first = utils.FirstOrDefault();
        return new ProbeTokenReport
        {
            Raw = token,
            Variants = variants,
            BaseToken = match.Current,
            Shortcut = generator.GetShortcutExpansion(token),
            Matched = utils.Length > 0,
            Rule = first is null ? null : ProbeValues.TypeName(configuration.Rules[first.Index].GetType()),
            RuleIndex = first?.Index,
            Rank = first?.Rank,
            Hints = hints,
            Styles = [.. utils.Select(util => DescribeStyle(util, positions[util], positionCount))],
        };
    }

    private static ProbeStyleReport DescribeStyle(StringifiedUtil util, int position, int positionCount)
    {
        var key = StyleOrdering.GetSortKey(util);
        return new ProbeStyleReport
        {
            Position = position,
            PositionCount = positionCount,
            Selector = ProbeValues.Selector(util.SelectorData),
            ContainerQuery = util.ContainerQueryData is null
                ? null
                : ProbeValues.ContainerQuery(util.ContainerQueryData),
            SortKey = new ProbeSortKeyReport
            {
                ClassCount = key.ClassCount,
                TypeCount = key.TypeCount,
                Layer = key.Layer,
                ContainerGroup = key.ContainerGroup,
                ContainerOrder = key.ContainerOrder,
                RuleIndex = key.RuleIndex,
                Rank = key.Rank,
                Sort = key.Sort,
                SelectorText = key.Text,
            },
            Setters =
            [
                .. util.Body.Select(setter => new ProbeSetterReport
                {
                    Property = setter.Property is null ? "null" : ProbeValues.Property(setter.Property),
                    Value = ProbeValues.Value(setter.Value),
                    ValueType = setter.Value?.GetType().FullName,
                    ResourceKey = setter.Value is DynamicResourceExtension dynamicResource
                        ? dynamicResource.ResourceKey?.ToString()
                        : null,
                }),
            ],
        };
    }

    /// <summary>
    /// Reconstructs the text each variant consumed by walking the handlers in match order and subtracting each
    /// handler's stripped token (<c>Matcher</c>) from the token as it stood before that match.
    /// </summary>
    private static ProbeVariantReport[] DescribeVariants(
        string raw,
        VariantMatchedResult<TTheme> match,
        AtomicConfiguration<TTheme> configuration
    )
    {
        var reports = new List<ProbeVariantReport>();
        var current = raw;

        // Handlers are prepended as each pass matches, so reversing recovers the order the variants were written in.
        foreach (var handler in match.Handlers.Reverse())
        {
            string? text = null;
            if (handler.Matcher is not null)
            {
                if (current.EndsWith(handler.Matcher, StringComparison.Ordinal))
                    text = current[..^handler.Matcher.Length];
                else if (current.StartsWith(handler.Matcher, StringComparison.Ordinal))
                    text = current[handler.Matcher.Length..];
                current = handler.Matcher;
            }

            var handlerName = ProbeValues.TypeName(handler.GetType());
            var variantName = handlerName.EndsWith("Handler", StringComparison.Ordinal)
                ? handlerName[..^"Handler".Length]
                : handlerName;
            var variant = configuration.Variants.FirstOrDefault(candidate =>
                ProbeValues.TypeName(candidate.GetType()) == variantName
            );

            reports.Add(
                new ProbeVariantReport
                {
                    Text = string.IsNullOrEmpty(text) ? null : text,
                    Variant = variant is null ? handlerName : ProbeValues.TypeName(variant.GetType()),
                    Handler = handlerName,
                }
            );
        }

        return [.. reports];
    }

    /// <summary>
    /// Cheap, non-authoritative diagnostics for an unmatched token. The useful case is a token whose variant
    /// prefix was recognised but whose base token resolved to no rule (<c>hover:nope-1</c>); the second case
    /// catches an unknown prefix in front of an otherwise valid token (<c>hver:ml-4</c>).
    /// </summary>
    private static string[] BuildHints(
        AtomicGenerator<TTheme> generator,
        AtomicConfiguration<TTheme> configuration,
        string token,
        VariantMatchedResult<TTheme> match,
        ProbeVariantReport[] variants
    )
    {
        var hints = new List<string>();

        if (variants.Length > 0)
        {
            hints.Add(
                $"Variant(s) {string.Join(", ", variants.Select(v => v.Text ?? v.Variant))} matched, but no rule matched the base token '{match.Current}'."
            );
            return [.. hints];
        }

        for (var i = 0; i < token.Length; i++)
        {
            if (token[i] != ':')
                continue;

            var prefix = token[..(i + 1)];
            var rest = token[(i + 1)..];
            if (rest.Length == 0)
                continue;

            var restUtils = generator.ParseToken(rest);
            if (restUtils.Length > 0)
            {
                hints.Add(
                    $"'{prefix}' is not a configured variant, but '{rest}' matches {ProbeValues.TypeName(configuration.Rules[restUtils[0].Index].GetType())}."
                );
                break;
            }
        }

        if (hints.Count == 0)
            hints.Add("No configured rule matched this token.");

        return [.. hints];
    }

    /// <summary>
    /// The resources the probed setters actually reference — not everything the generator happened to resolve
    /// (a hint lookup can resolve an unrelated token). Each <c>ThemeAccess</c> is compiled and invoked once
    /// against the live theme, exactly as <see cref="ResourceDictionaryEmitter"/> does when emitting.
    /// </summary>
    private static ProbeResourceInfo[] ResolveResources(
        AtomicGenerator<TTheme> generator,
        IReadOnlyList<StringifiedUtil> ordered
    )
    {
        var keys = ordered
            .SelectMany(util => util.Body)
            .Select(setter => (setter.Value as DynamicResourceExtension)?.ResourceKey?.ToString())
            .Where(key => !string.IsNullOrEmpty(key))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal);

        var infos = new List<ProbeResourceInfo>();
        foreach (var key in keys)
        {
            if (key is null || !generator.ResolvedResources.TryGetValue(key, out var resource))
                continue;

            var resolved = resource.ThemeAccess.Compile().DynamicInvoke(generator.Configuration.Theme);
            var isThemed = resolved is IThemedValue { IsThemed: true };
            infos.Add(
                new ProbeResourceInfo
                {
                    Key = key,
                    Themed = isThemed,
                    Resolved = resolved,
                }
            );
        }

        return [.. infos];
    }
}

/// <summary>A referenced resource key plus its live theme value — the data the control runner builds a dictionary from.</summary>
internal sealed record ProbeResourceInfo
{
    public required string Key { get; init; }
    public required bool Themed { get; init; }
    public required object? Resolved { get; init; }
}
