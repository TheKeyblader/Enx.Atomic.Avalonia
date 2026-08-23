using Avalonia.Media;
using Enx.Atomic.Avalonia.Preset.Mini.Dynamic;
using Enx.Atomic.Avalonia.Preset.Mini.Variants;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>
/// Fluent <see cref="ThemeBuilder{TTheme}"/> extensions that seed each theme part (see <c>Parts/</c>) with its
/// <see cref="DefaultTheme"/> scale and register the <see cref="Rule"/>s/<see cref="VariantBase{TTheme}"/>s that
/// depend on it into the given <see cref="AtomicConfiguration{TTheme}"/> — one <c>Add*Rules</c> method per part,
/// generic over any <typeparamref name="TTheme"/> implementing that part, plus <see cref="AddMiniTheme"/> which
/// wires all of them together for <see cref="MiniTheme"/> specifically.
/// </summary>
public static class ThemeBuilderExtensions
{
    /// <summary>Adds <paramref name="rule"/> to <paramref name="configuration"/> unless a rule of the same type is already registered — the rule types here are plain classes without value equality, so dedup has to go by type rather than by <see cref="List{T}.Contains"/>.</summary>
    private static void AddRuleOnce<TTheme>(this AtomicConfiguration<TTheme> configuration, IRule rule)
        where TTheme : class
    {
        if (configuration.Rules.All(r => r.GetType() != rule.GetType()))
            configuration.Rules.Add(rule);
    }

    /// <summary>Adds <paramref name="variant"/> to <paramref name="configuration"/> unless a variant of the same type is already registered — see <see cref="AddRuleOnce{TTheme}"/>.</summary>
    private static void AddVariantOnce<TTheme>(this AtomicConfiguration<TTheme> configuration, VariantBase<TTheme> variant)
        where TTheme : class
    {
        if (configuration.Variants.All(v => v.GetType() != variant.GetType()))
            configuration.Variants.Add(variant);
    }

    /// <summary>Adds <paramref name="transformer"/> to <paramref name="configuration"/> unless a transformer of the same type is already registered — see <see cref="AddRuleOnce{TTheme}"/>.</summary>
    private static void AddTransformerOnce<TTheme>(
        this AtomicConfiguration<TTheme> configuration,
        ISourceTransformer<TTheme> transformer
    )
        where TTheme : class
    {
        if (configuration.Transformers.All(t => t.GetType() != transformer.GetType()))
            configuration.Transformers.Add(transformer);
    }

    /// <summary>
    /// Adds <paramref name="rule"/> to <paramref name="configuration"/> unless a static rule with the same
    /// <see cref="IStaticRule.Name"/> is already registered. Unlike the dynamic rules, every <c>Static/</c> rule
    /// shares the same CLR type (<see cref="Rule.Static"/>), so dedup here has to go by name instead of by type.
    /// </summary>
    private static void AddStaticRuleOnce<TTheme>(this AtomicConfiguration<TTheme> configuration, Rule.Static rule)
        where TTheme : class
    {
        if (configuration.Rules.OfType<IStaticRule>().All(r => r.Name != rule.Name))
            configuration.Rules.Add(rule);
    }

    public static ThemeBuilder<TTheme> AddSpacingRules<TTheme>(
        this ThemeBuilder<TTheme> builder,
        AtomicConfiguration<TTheme> configuration
    )
        where TTheme : class, ISpacingPart, IRemToPxPart, new()
    {
        foreach (var (key, value) in DefaultTheme.Spacing)
            builder.Theme.Spacing[key] = value;

        configuration.AddRuleOnce(new MarginRule<TTheme>());
        configuration.AddRuleOnce(new PaddingRule<TTheme>());
        configuration.AddRuleOnce(new GapRule<TTheme>());
        return builder;
    }

    public static ThemeBuilder<TTheme> AddSizeRules<TTheme>(
        this ThemeBuilder<TTheme> builder,
        AtomicConfiguration<TTheme> configuration
    )
        where TTheme : class, ISizePart, IRemToPxPart, new()
    {
        foreach (var (key, value) in DefaultTheme.Sizes)
            builder.Theme.Sizes[key] = value;

        configuration.AddRuleOnce(new SizeRule<TTheme>());
        return builder;
    }

    public static ThemeBuilder<TTheme> AddRadiusRules<TTheme>(
        this ThemeBuilder<TTheme> builder,
        AtomicConfiguration<TTheme> configuration
    )
        where TTheme : class, IRadiusPart, IRemToPxPart, new()
    {
        foreach (var (key, value) in DefaultTheme.Radii)
            builder.Theme.Radii[key] = value;

        configuration.AddRuleOnce(new RoundedRule<TTheme>());
        return builder;
    }

    /// <summary>Also registers <see cref="BorderWidthRule{TTheme}"/> — add before <see cref="AddColorRules{TTheme}"/> so it wins the shared <c>border-</c> prefix over <see cref="BorderColorRule{TTheme}"/>.</summary>
    public static ThemeBuilder<TTheme> AddLineWidthRules<TTheme>(
        this ThemeBuilder<TTheme> builder,
        AtomicConfiguration<TTheme> configuration
    )
        where TTheme : class, ILineWidthPart, new()
    {
        foreach (var (key, value) in DefaultTheme.LineWidths)
            builder.Theme.LineWidths[key] = value;

        configuration.AddRuleOnce(new BorderWidthRule<TTheme>());
        return builder;
    }

    /// <summary>Also registers <see cref="FontSizeRule{TTheme}"/> — add before <see cref="AddColorRules{TTheme}"/> so it wins the shared <c>text-</c> prefix over <see cref="ForegroundColorRule{TTheme}"/>.</summary>
    public static ThemeBuilder<TTheme> AddFontSizeRules<TTheme>(
        this ThemeBuilder<TTheme> builder,
        AtomicConfiguration<TTheme> configuration
    )
        where TTheme : class, IFontSizePart, IRemToPxPart, new()
    {
        foreach (var (key, value) in DefaultTheme.FontSizes)
            builder.Theme.FontSizes[key] = value;

        configuration.AddRuleOnce(new FontSizeRule<TTheme>());
        return builder;
    }

    /// <summary>Also registers <see cref="BreakpointVariant{TTheme}"/> onto <see cref="AtomicConfiguration{TTheme}.Variants"/>.</summary>
    public static ThemeBuilder<TTheme> AddBreakpointRules<TTheme>(
        this ThemeBuilder<TTheme> builder,
        AtomicConfiguration<TTheme> configuration
    )
        where TTheme : class, IBreakpointPart, new()
    {
        foreach (var (key, value) in DefaultTheme.Breakpoints)
            builder.Theme.Breakpoints[key] = value;

        configuration.AddVariantOnce(new BreakpointVariant<TTheme>());
        return builder;
    }

    public static ThemeBuilder<TTheme> AddColorRules<TTheme>(
        this ThemeBuilder<TTheme> builder,
        AtomicConfiguration<TTheme> configuration
    )
        where TTheme : class, IColorPart, new()
    {
        foreach (var (key, hex) in DefaultTheme.ColorHex)
            builder.Theme.Colors[key] = new SolidColorBrush(Color.Parse(hex));

        configuration.AddRuleOnce(new BackgroundColorRule<TTheme>());
        configuration.AddRuleOnce(new ForegroundColorRule<TTheme>());
        configuration.AddRuleOnce(new BorderColorRule<TTheme>());
        return builder;
    }

    /// <summary>Registers <see cref="PseudoClassVariant{TTheme}"/> onto <see cref="AtomicConfiguration{TTheme}.Variants"/>. Not tied to any theme part.</summary>
    public static ThemeBuilder<TTheme> AddPseudoClassVariants<TTheme>(
        this ThemeBuilder<TTheme> builder,
        AtomicConfiguration<TTheme> configuration
    )
        where TTheme : class, new()
    {
        configuration.AddVariantOnce(new PseudoClassVariant<TTheme>());
        return builder;
    }

    /// <summary>Every rule declared under <c>Static/</c> — theme-independent, so valid for any <typeparamref name="TTheme"/>.</summary>
    private static readonly Rule.Static[] AllStaticRules =
    [
        .. Visibility.All,
        .. PointerEvents.All,
        .. Interactivity.All,
        .. Clipping.All,
        .. Direction.All,
        .. Cursors.All,
        .. FontStyles.All,
        .. FontWeights.All,
        .. TextAlign.All,
        .. TextDecoration.All,
        .. TextWrap.All,
        .. ObjectFit.All,
        .. FlexDirection.All,
        .. SelfAlign.All,
        .. ItemsAlign.All,
        .. ScrollBarVisibilityRules.All,
    ];

    /// <summary>Registers every theme-independent <c>Static/</c> rule into <paramref name="configuration"/>.</summary>
    public static ThemeBuilder<TTheme> AddStaticRules<TTheme>(
        this ThemeBuilder<TTheme> builder,
        AtomicConfiguration<TTheme> configuration
    )
        where TTheme : class, new()
    {
        foreach (var rule in AllStaticRules)
            configuration.AddStaticRuleOnce(rule);
        return builder;
    }

    public static ThemeBuilder<TTheme> AddRemToPxFactor<TTheme>(this ThemeBuilder<TTheme> builder, float factor = 16f)
        where TTheme : class, IRemToPxPart, new()
    {
        builder.Theme.RemToPxFactor = factor;
        return builder;
    }

    /// <summary>
    /// Registers <see cref="GhostPropertyCombiner{TTheme}"/> onto <see cref="AtomicConfiguration{TTheme}.Transformers"/>.
    /// Without it, the ghost-property branches of <c>MarginRule</c>/<c>PaddingRule</c>/<c>RoundedRule</c>
    /// (e.g. <c>ml-4</c>) never resolve to anything real. Not tied to any theme part.
    /// </summary>
    public static ThemeBuilder<TTheme> AddGhostPropertyCombiner<TTheme>(
        this ThemeBuilder<TTheme> builder,
        AtomicConfiguration<TTheme> configuration
    )
        where TTheme : class, new()
    {
        configuration.AddTransformerOnce(new GhostPropertyCombiner<TTheme>());
        return builder;
    }

    /// <summary>Seeds every part <see cref="MiniTheme"/> implements with its <see cref="DefaultTheme"/> scale, and registers every rule/variant/transformer depending on it into <paramref name="configuration"/>.</summary>
    public static ThemeBuilder<MiniTheme> AddMiniTheme(
        this ThemeBuilder<MiniTheme> builder,
        AtomicConfiguration<MiniTheme> configuration
    ) =>
        builder
            .AddStaticRules(configuration)
            .AddSpacingRules(configuration)
            .AddSizeRules(configuration)
            .AddRadiusRules(configuration)
            .AddLineWidthRules(configuration)
            .AddFontSizeRules(configuration)
            .AddBreakpointRules(configuration)
            .AddColorRules(configuration)
            .AddPseudoClassVariants(configuration)
            .AddGhostPropertyCombiner(configuration)
            .AddRemToPxFactor();
}
