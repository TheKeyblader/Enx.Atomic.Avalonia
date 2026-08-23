namespace Enx.Atomic.Avalonia;

/// <summary>
/// The full set of building blocks an <see cref="AtomicGenerator{TTheme}"/> resolves utility tokens against:
/// pre-processors, rules, extractors, variants, the theme instance, and value emitters.
/// </summary>
public class AtomicConfiguration<TTheme>
    where TTheme : class
{
    /// <summary>Full-text source rewriters, run before extraction — see <see cref="ISourceTransformer{TTheme}"/>.</summary>
    public List<ISourceTransformer<TTheme>> Transformers { get; set; } = [];

    /// <summary>Token rewriters run, in list order, before variant matching.</summary>
    public List<IPreProcessor> PreProcessors { get; set; } = [];

    /// <summary>Static and dynamic rules that turn a variant-stripped token into style values.</summary>
    public List<IRule> Rules { get; set; } = [];

    /// <summary>Extractors that pull candidate utility tokens out of source text. Defaults to a single <see cref="SplitExtractor"/>.</summary>
    public List<Extractor> Extractors { get; set; } = [new SplitExtractor()];

    /// <summary>Variants (e.g. <c>hover:</c>, <c>dark:</c>) that can be stripped from the front of a token before rule matching.</summary>
    public List<VariantBase<TTheme>> Variants { get; set; } = [];

    /// <summary>The theme instance rules and variants can read values from (colors, spacing, breakpoints, etc.).</summary>
    public required TTheme Theme { get; set; }

    /// <summary>The container name used when emitting container-query-scoped styles.</summary>
    public string ContainerName { get; set; } = "top-level";
}