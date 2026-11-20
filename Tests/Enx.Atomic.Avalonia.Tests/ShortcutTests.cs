using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Enx.Atomic.Avalonia.Compact;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// Shortcuts: a named token expanding to other utilities before rule matching (UnoCSS's <c>shortcuts</c>).
/// Asserted on a real shown window wherever a property can apply at runtime — the emitted order is the
/// cascade, so these tests also pin the <see cref="StyleLayer"/> contract (shortcut styles before plain
/// utilities, specificity still compared first).
/// </summary>
public class ShortcutTests
{
    /// <summary>
    /// A static shortcut is one style per expanded token (never merged), all selecting on the class the user
    /// wrote — and the inner tokens are not emitted under their own names.
    /// </summary>
    [AvaloniaFact]
    public void StaticShortcut_ExpandsToIndependentStylesOnTheWrittenClass()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("done", "line-through  opacity-45 line-through"));

        var utils = Generate(generator, "done");

        Assert.Equal(2, utils.Length);
        Assert.All(utils, util => Assert.Contains("done", ClassNames(util)));
        Assert.DoesNotContain(utils, util => ClassNames(util).Contains("opacity-45"));
        Assert.Equal(new[] { "line-through", "opacity-45" }, generator.GetShortcutExpansion("done"));

        var text = new TextBlock { Text = "done", Classes = { "done" } };
        var window = utils.ShowWithStyles(text);
        try
        {
            Assert.Equal(TextDecorations.Strikethrough, text.TextDecorations);
            Assert.Equal(0.45, text.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// An outer variant stays attached to the written token: <c>hover:btn</c> selects
    /// <c>.hover\:btn:pointerover</c>, not <c>.btn:pointerover</c>, so the control has to carry the exact class
    /// the user typed (as for a plain utility), and the variant applies on top of the shortcut's own utilities.
    /// </summary>
    [AvaloniaFact]
    public void OuterVariant_TargetsTheWrittenClassAndAppliesOnTop()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("btn", "bg-red-500 opacity-40"));

        var shortcutUtils = generator.ParseToken("hover:btn");
        Assert.All(
            shortcutUtils,
            util =>
            {
                Assert.Contains("hover:btn", ClassNames(util));
                Assert.Contains(":pointerover", ClassNames(util));
                Assert.DoesNotContain("btn", ClassNames(util));
            }
        );
        Assert.Contains("Colors[red-500]", generator.ResolvedResources.Keys);

        var utils = generator.Generate(
            "Classes=\"hover:btn\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );
        var text = new TextBlock { Text = "btn", Classes = { "hover:btn" } };
        var window = utils.ShowWithStyles(text);
        try
        {
            // At rest neither the variant nor the shortcut's base styles apply: the selector needs :pointerover.
            Assert.Equal(1, text.Opacity);

            text.SetPseudoClass(":pointerover", true);
            Assert.Equal(0.4, text.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>A variant written inside a shortcut wraps the shortcut's class, e.g. <c>fade</c> = <c>hover:opacity-50</c>.</summary>
    [AvaloniaFact]
    public void InnerVariant_AppliesOnTheShortcutClass()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("fade", "hover:opacity-50"));

        var utils = Generate(generator, "fade");
        var text = new TextBlock { Text = "fade", Classes = { "fade" } };
        var window = utils.ShowWithStyles(text);
        try
        {
            Assert.Equal(1, text.Opacity);

            text.SetPseudoClass(":pointerover", true);
            Assert.Equal(0.5, text.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Shortcuts compose: a shortcut may expand to another shortcut, and either level may carry variants —
    /// the inner handlers combine with the outer ones so <c>hover:outer</c> (outer → inner → opacity) and a
    /// shortcut containing <c>hover:inner</c> both land on the written class with <c>:pointerover</c>.
    /// </summary>
    [AvaloniaFact]
    public void NestedShortcut_CombinesInnerAndOuterVariants()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("inner", "opacity-50"));
        configuration.Shortcuts.Add(Shortcut.Static("outer", "inner"));
        configuration.Shortcuts.Add(Shortcut.Static("outer-hover", "hover:inner"));

        var utils = Generate(generator, "outer outer-hover hover:outer");
        var plain = new TextBlock { Text = "outer", Classes = { "outer" } };
        var withInnerVariant = new TextBlock { Text = "outer-hover", Classes = { "outer-hover" } };
        var writtenOuterVariant = new TextBlock { Text = "hover:outer", Classes = { "hover:outer" } };

        var window = utils.ShowWithStyles(plain, withInnerVariant, writtenOuterVariant);
        try
        {
            Assert.Equal(0.5, plain.Opacity);
            Assert.Contains("outer-hover", ClassNames(Assert.Single(generator.ParseToken("outer-hover"))));

            Assert.Equal(1, withInnerVariant.Opacity);
            Assert.Equal(1, writtenOuterVariant.Opacity);

            withInnerVariant.SetPseudoClass(":pointerover", true);
            writtenOuterVariant.SetPseudoClass(":pointerover", true);
            Assert.Equal(0.5, withInnerVariant.Opacity);
            Assert.Equal(0.5, writtenOuterVariant.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Two leaves sharing a base token but not their variants (<c>inner</c> and <c>hover:inner</c>, both ending in
    /// <c>opacity-50</c>) are distinct utilities: deduplication keys on the token as rewritten with its variants,
    /// like UnoCSS's <c>uniq</c> over the rewritten strings, so the hovered one isn't silently dropped.
    /// </summary>
    [AvaloniaFact]
    public void NestedShortcut_KeepsLeavesThatOnlyDifferByVariant()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("inner", "opacity-50"));
        configuration.Shortcuts.Add(Shortcut.Static("combo", "inner hover:inner"));

        var utils = Generate(generator, "combo");

        Assert.Equal(2, utils.Length);
        Assert.Equal(new[] { "opacity-50", "hover:opacity-50" }, generator.GetShortcutExpansion("combo"));
        Assert.Contains(utils, util => ClassNames(util).Contains(":pointerover"));
    }

    /// <summary>
    /// A regex shortcut computes its expansion from the match; a token it doesn't match falls through to the
    /// rules untouched (here a rule named exactly <c>btn</c>), proving shortcut matching is tried first but
    /// doesn't shadow non-matching tokens. The collection-returning factory overload is covered too.
    /// </summary>
    [AvaloniaFact]
    public void DynamicShortcut_ResolvesAndNonMatchingTokenFallsThroughToRules()
    {
        var (configuration, generator) = CreateGenerator(configuration =>
        {
            configuration.Rules.Add(new Rule.Static("btn", [Visual.OpacityProperty.ToLiteral(0.25)]));
            configuration.Shortcuts.Add(
                Shortcut.Dynamic<MiniTheme>(
                    new Regex("^btn-(.+)$"),
                    (match, _) => $"bg-{match.Groups[1].Value}-500 px-4"
                )
            );
            configuration.Shortcuts.Add(
                Shortcut.Dynamic<MiniTheme>(
                    new Regex("^fx-(.+)$"),
                    (match, _) => [$"opacity-{match.Groups[1].Value}", "line-through"]
                )
            );
        });

        Assert.Null(generator.GetShortcutExpansion("btn"));

        var utils = Generate(generator, "btn-red btn fx-30");
        Assert.Contains("Colors[red-500]", generator.ResolvedResources.Keys);
        Assert.Equal(new[] { "bg-red-500", "px-4" }, generator.GetShortcutExpansion("btn-red"));
        Assert.Equal(new[] { "opacity-30", "line-through" }, generator.GetShortcutExpansion("fx-30"));
        Assert.Null(generator.GetShortcutExpansion("btn"));

        var colored = new Border { Classes = { "btn-red" } };
        var ruleBacked = new TextBlock { Text = "btn", Classes = { "btn" } };
        var collectionBacked = new TextBlock { Text = "fx-30", Classes = { "fx-30" } };
        var window = utils.ShowWithStyles(colored, ruleBacked, collectionBacked);
        try
        {
            // The regex match wins over any rule for the same token, and "btn" (no match) still resolves
            // through the rule it names.
            Assert.Equal(new Thickness(16, 0, 16, 0), colored.Padding);
            Assert.Equal(0.25, ruleBacked.Opacity);
            Assert.Equal(0.3, collectionBacked.Opacity);
            Assert.Equal(TextDecorations.Strikethrough, collectionBacked.TextDecorations);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>A shortcut chain that refers to itself must fail loudly, naming the chain, instead of recursing forever.</summary>
    [AvaloniaFact]
    public void CyclicShortcuts_ThrowNamingTheChain()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("a", "b opacity-50"));
        configuration.Shortcuts.Add(Shortcut.Static("b", "a"));

        var exception = Assert.Throws<InvalidOperationException>(() => generator.ParseToken("a"));

        Assert.Contains("a -> b -> a", exception.Message);
        Assert.Contains("nested shortcuts", exception.Message);
    }

    /// <summary>
    /// An expansion token nothing matches is a configuration typo: it lands in <see cref="AtomicGenerator{TTheme}.Diagnostics"/>
    /// (deduplicated) and the rest of the shortcut still resolves — a build must not fail on it.
    /// </summary>
    [AvaloniaFact]
    public void UnmatchedInnerToken_IsDiagnosedAndTheRestStillApplies()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("card", "bg-rde-500 opacity-50"));

        var utils = Generate(generator, "card");

        Assert.Single(utils);
        Assert.Equal(new[] { "unmatched utility \"bg-rde-500\" in shortcut \"card\"" }, generator.Diagnostics);

        var text = new TextBlock { Text = "card", Classes = { "card" } };
        var window = utils.ShowWithStyles(text);
        try
        {
            Assert.Equal(0.5, text.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// The shortcut layer: at equal specificity a plain utility outranks a shortcut's utility regardless of the
    /// order the two classes are written in (UnoCSS emits <c>shortcuts</c> before <c>default</c>).
    /// </summary>
    [AvaloniaFact]
    public void Layer_PlainUtilityWinsOverAnEquallySpecificShortcutUtility()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("btn", "opacity-50"));

        Assert.Equal(StyleLayer.Shortcuts, Assert.Single(generator.ParseToken("btn")).Layer);
        Assert.Equal(StyleLayer.Default, Assert.Single(generator.ParseToken("opacity-100")).Layer);

        var shortcutFirst = new TextBlock { Text = "a", Classes = { "btn", "opacity-100" } };
        var utilityFirst = new TextBlock { Text = "b", Classes = { "opacity-100", "btn" } };
        var window = Generate(generator, "btn opacity-100").ShowWithStyles(shortcutFirst, utilityFirst);
        try
        {
            Assert.Equal(1, shortcutFirst.Opacity);
            Assert.Equal(1, utilityFirst.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// A <c>sm:</c> variant inside a shortcut doesn't raise its specificity, so at a wide container the plain
    /// utility still wins — the <c>class="btn bg-red-500"</c> case from UnoCSS — while the shortcut applies on
    /// its own.
    /// </summary>
    [AvaloniaFact]
    public void Layer_ShortcutContainingABreakpointLosesToAPlainUtility()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("sm-fade", "sm:opacity-50"));

        var both = new TextBlock { Text = "both", Classes = { "sm-fade", "opacity-100" } };
        var shortcutOnly = new TextBlock { Text = "shortcut", Classes = { "sm-fade" } };
        var window = Generate(generator, "sm-fade opacity-100").ShowWithStyles(800, both, shortcutOnly);
        try
        {
            Assert.Equal(1, both.Opacity);
            Assert.Equal(0.5, shortcutOnly.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Specificity is compared before the layer, so a shortcut written with <c>hover:</c> (two class-like
    /// nodes) still beats a plain utility, exactly as for a plain <c>hover:</c> utility.
    /// </summary>
    [AvaloniaFact]
    public void Layer_MoreSpecificShortcutStillBeatsAPlainUtility()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("btn", "opacity-50"));

        var text = new TextBlock { Text = "btn", Classes = { "hover:btn", "opacity-100" } };
        var window = Generate(generator, "hover:btn opacity-100").ShowWithStyles(text);
        try
        {
            Assert.Equal(1, text.Opacity);

            text.SetPseudoClass(":pointerover", true);
            Assert.Equal(0.5, text.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Utilities inside a shortcut keep their own rule ranks, so <c>p-2 px-4</c> composes the composite padding
    /// sides exactly as the two plain tokens would (uniform before axis): 8px vertical, 16px horizontal.
    /// </summary>
    [AvaloniaFact]
    public void CompositeInsideShortcut_KeepsTheUniformBeforeAxisOrder()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("card", "p-2 px-4"));

        var utils = Generate(generator, "card");
        var border = new Border { Classes = { "card" } };
        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(new Thickness(16, 8, 16, 8), border.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>A shortcut takes precedence over a rule with the same name, as in UnoCSS.</summary>
    [AvaloniaFact]
    public void Shortcut_BeatsARuleWithTheSameName()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("hidden", "opacity-50"));

        var utils = Generate(generator, "hidden");
        var text = new TextBlock { Text = "hidden", Classes = { "hidden" } };
        var window = utils.ShowWithStyles(text);
        try
        {
            Assert.True(text.IsVisible);
            Assert.Equal(0.5, text.Opacity);
            Assert.DoesNotContain(utils, util => util.Body.Any(setter => setter.Property == Visual.IsVisibleProperty));
        }
        finally
        {
            window.Close();
        }
    }

    private static (AtomicConfiguration<MiniTheme> Configuration, AtomicGenerator<MiniTheme> Generator) CreateGenerator(
        Action<AtomicConfiguration<MiniTheme>> configure
    )
    {
        var builder = ThemeBuilder<MiniTheme>.Create();
        var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };
        builder.AddMiniTheme(configuration);
        configure(configuration);
        return (configuration, new AtomicGenerator<MiniTheme>(configuration));
    }

    private static StringifiedUtil[] Generate(AtomicGenerator<MiniTheme> generator, string classes) =>
        generator.Generate($"Classes=\"{classes}\"", new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" });

    /// <summary>The class-like selector nodes the style selects on, in chain order.</summary>
    private static List<string> ClassNames(StringifiedUtil util)
    {
        List<string> names = [];
        for (var node = util.SelectorData; node is not null; node = node.Previous)
        {
            if (node is SelectorExpression.Class { Name: { } name })
                names.Add(name);
        }

        return names;
    }
}
