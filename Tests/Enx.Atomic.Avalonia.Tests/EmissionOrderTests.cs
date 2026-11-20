using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Threading;
using Enx.Atomic.Avalonia.CodeGen;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// Bug 8: a style whose selector carries an activator is framed at <c>BindingPriority.StyleTrigger</c>
/// (<c>StyleInstance.GetPriority</c>), and a plain class selector <em>is</em> an activator — so <c>.w-4</c> and
/// <c>.hover:w-8:pointerover</c> tie, and the frame inserted last wins (<c>ValueStore.InsertFrame</c> appends,
/// <c>ReevaluateEffectiveValues</c> walks frames back-to-front). Avalonia has no specificity, so the engine has to
/// encode the CSS cascade into emission order: base utilities before pseudo-class variants, wider breakpoints before
/// narrower ones, more specific selectors last. These tests drive the real generator and assert the property values
/// a shown window actually ends up with.
/// </summary>
public class EmissionOrderTests
{
    /// <summary>Reusing a rule in another configuration must not change its position in the original cascade.</summary>
    [AvaloniaFact]
    public void SharedRules_KeepTheirOwnGeneratorsDeclarationOrder()
    {
        var left = new Rule.Static("left", [Border.WidthProperty.ToLiteral(16d)]);
        var right = new Rule.Static("right", [Border.WidthProperty.ToLiteral(32d)]);
        var theme = new object();
        var first = new AtomicGenerator<object>(
            new AtomicConfiguration<object> { Theme = theme, Rules = [left, right] }
        );
        var second = new AtomicGenerator<object>(
            new AtomicConfiguration<object> { Theme = theme, Rules = [right, left] }
        );

        Assert.Equal(0, StyleOrdering.GetSortKey(Assert.Single(first.ParseToken("left"))).RuleIndex);
        Assert.Equal(1, StyleOrdering.GetSortKey(Assert.Single(first.ParseToken("right"))).RuleIndex);
        Assert.Equal(1, StyleOrdering.GetSortKey(Assert.Single(second.ParseToken("left"))).RuleIndex);
        Assert.Equal(0, StyleOrdering.GetSortKey(Assert.Single(second.ParseToken("right"))).RuleIndex);

        var border = new Border { Classes = { "right", "left" } };
        var window = first.Generate("right left", new()).ShowWithStyles(border);
        try
        {
            Assert.Equal(32, border.Width);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>An empty static match does not shadow a dynamic match or change its declaration index.</summary>
    [AvaloniaFact]
    public void EmptyStaticRule_FallsThroughToTheDynamicRuleAtItsRegisteredIndex()
    {
        var generator = new AtomicGenerator<object>(
            new AtomicConfiguration<object>
            {
                Theme = new(),
                Rules =
                [
                    new Rule.Static("size", []),
                    new Rule.Dynamic<object>(new Regex("^size$"), (_, _) => [Border.WidthProperty.ToLiteral(32d)]),
                ],
            }
        );

        var util = Assert.Single(generator.ParseToken("size"));
        Assert.Equal(1, StyleOrdering.GetSortKey(util).RuleIndex);
        Assert.Equal(32d, Assert.Single(util.Body).Value);
    }

    /// <summary>
    /// The failure case: the variant is written <em>before</em> its base. Emission order must not depend on source
    /// order, so <c>hover:w-8 w-4</c> must behave exactly like <c>w-4 hover:w-8</c>: 16 at rest, 32 while hovered.
    /// </summary>
    [AvaloniaFact]
    public void HoverVariantWrittenFirst_StillWinsOverItsBase()
    {
        var utils = Generate("hover:w-8 w-4");
        var border = new Border { Classes = { "hover:w-8", "w-4" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(16, border.Width);

            border.SetPseudoClass(":pointerover", true);
            Assert.Equal(32, border.Width);

            border.SetPseudoClass(":pointerover", false);
            Assert.Equal(16, border.Width);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Guard: the order that already worked must keep working.</summary>
    [AvaloniaFact]
    public void BaseWrittenFirst_HoverStillWins()
    {
        var utils = Generate("w-4 hover:w-8");
        var border = new Border { Classes = { "w-4", "hover:w-8" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(16, border.Width);

            border.SetPseudoClass(":pointerover", true);
            Assert.Equal(32, border.Width);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// A breakpoint written before its base: while the container is wide enough for <c>sm</c> the scoped value must
    /// win (32), and once it drops below 640 only the base applies (16). The container sizing is set up by
    /// <see cref="TestHelpers.ShowWithStyles(IEnumerable{StringifiedUtil}, double, Control[])"/>.
    /// </summary>
    [AvaloniaFact]
    public void BreakpointWrittenFirst_StillWinsOnAWideContainer()
    {
        var utils = Generate("sm:w-8 w-4");
        var border = new Border { Classes = { "sm:w-8", "w-4" } };

        var window = utils.ShowWithStyles(800, border);
        try
        {
            Assert.Equal(32, border.Width);

            window.Width = 500;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(16, border.Width);

            window.Width = 800;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(32, border.Width);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Selector specificity outranks container order: <c>.hover\:w-8:pointerover</c> (two classes) beats
    /// <c>.sm\:w-6</c> (one class) even though the breakpoint is the *more specific condition* in CSS terms — in CSS
    /// specificity is compared before the at-rule's position, and here the hover style must be emitted last.
    /// </summary>
    [AvaloniaFact]
    public void PseudoClassSpecificity_OutranksBreakpointContainerOrder()
    {
        var utils = Generate("sm:w-6 hover:w-8");
        var border = new Border { Classes = { "sm:w-6", "hover:w-8" } };

        var window = utils.ShowWithStyles(800, border);
        try
        {
            // Wide, not hovered: only the breakpoint applies (24).
            Assert.Equal(24, border.Width);

            // Wide and hovered: both apply, and the more specific selector wins (32).
            border.SetPseudoClass(":pointerover", true);
            Assert.Equal(32, border.Width);

            // Narrow and hovered: the breakpoint no longer applies, hover still does.
            window.Width = 500;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(32, border.Width);

            // Narrow, not hovered: nothing applies, back to the unset value.
            border.SetPseudoClass(":pointerover", false);
            Assert.Equal(double.NaN, border.Width);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// The upper-bound counterpart: <c>max-sm:</c> is a "width at most 640" condition, so it must still be emitted
    /// after an unconditional base utility (32 while narrow, 16 once the container is wide enough that only the
    /// base applies). Among several upper bounds the narrowest wins, which is why the group orders them by
    /// descending threshold.
    /// </summary>
    [AvaloniaFact]
    public void MaxBreakpointWrittenFirst_StillWinsOnANarrowContainer()
    {
        var utils = Generate("max-sm:w-8 w-4");
        var border = new Border { Classes = { "max-sm:w-8", "w-4" } };

        var window = utils.ShowWithStyles(500, border);
        try
        {
            Assert.Equal(32, border.Width);

            window.Width = 800;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(16, border.Width);

            window.Width = 500;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(32, border.Width);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Two breakpoints written in reverse: both match at 800, and the larger one (<c>md</c>, 32) must win because
    /// wider breakpoints are emitted later — mirroring CSS, where the ascending media blocks put <c>md</c> after
    /// <c>sm</c>.
    /// </summary>
    [AvaloniaFact]
    public void TwoBreakpointsWrittenInReverse_LargerBreakpointWins()
    {
        var utils = Generate("md:w-8 sm:w-6");
        var border = new Border { Classes = { "md:w-8", "sm:w-6" } };

        var window = utils.ShowWithStyles(800, border);
        try
        {
            Assert.Equal(32, border.Width);

            window.Width = 700;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(24, border.Width);

            window.Width = 500;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(double.NaN, border.Width);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Across source files: <c>AtomicCli</c> resolves each file separately, so a per-file sort is not enough — the
    /// concatenation has to be sorted globally, or a variant emitted by the first file ends up before a base
    /// utility emitted by the second one and the base wins the tie. Asserted on the emitted text, in emission order.
    /// </summary>
    [AvaloniaFact]
    public void AcrossSourceFiles_BaseIsEmittedBeforeItsVariant()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();

        var directory = Path.Combine(Path.GetTempPath(), $"enx-atomic-emission-order-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            // The first file carries only the variant, the second only its base: resolving per file and
            // concatenating yields [hover, base], the exact order bug 8 is about.
            var first = Path.Combine(directory, "First.axaml");
            var second = Path.Combine(directory, "Second.axaml");
            var output = Path.Combine(directory, "AtomicStyles.g.cs");
            File.WriteAllText(first, "<Border Classes=\"hover:w-8\" />");
            File.WriteAllText(second, "<Border Classes=\"w-4\" />");

            var exitCode = AtomicCli.Run(["--output", output, first, second], configuration);
            Assert.Equal(0, exitCode);

            var emitted = File.ReadAllText(output);
            var baseIndex = emitted.IndexOf(".Class(\"w-4\")", StringComparison.Ordinal);
            var hoverIndex = emitted.IndexOf(".Class(\"hover:w-8\")", StringComparison.Ordinal);
            Assert.True(baseIndex >= 0, emitted);
            Assert.True(hoverIndex >= 0, emitted);
            Assert.True(
                baseIndex < hoverIndex,
                $"Expected '.Class(\"w-4\")' before '.Class(\"hover:w-8\")', got:\n{emitted}"
            );
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static StringifiedUtil[] Generate(string classes)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        return generator.Generate(
            $"Classes=\"{classes}\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );
    }
}
