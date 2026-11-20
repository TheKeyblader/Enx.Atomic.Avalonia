using Avalonia;
using Avalonia.Controls;
using Enx.Atomic.Avalonia.CodeGen;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// One red regression test per confirmed bug. Each asserts the <em>correct</em> behavior and is expected to fail
/// today; a bug test and its guard are split into separate methods so each failure points at exactly one bug.
/// Where feasible the assertions read real property values off a shown headless window (see
/// <see cref="TestHelpers.ShowWithStyles"/>) rather than only inspecting the generated setter payload.
/// </summary>
public class KnownBugsRegressionTests
{
    /// <summary>
    /// Bug 1: a variant on a ghost-property token is lost by <c>GhostPropertyCombiner</c>, which rebuilds the
    /// selector from class names alone. <c>hover:ml-4</c> must only apply under <c>:pointerover</c>; today the
    /// generated style has no pseudo-class, so the margin is applied unconditionally.
    /// </summary>
    [AvaloniaFact]
    public void HoverGhostMargin_AppliesOnlyWhilePointerOver()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"hover:ml-4\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var util = Assert.Single(utils);

        var textBlock = new TextBlock { Classes = { "hover:ml-4" } };
        var window = utils.ShowWithStyles(textBlock);
        try
        {
            Assert.Equal(default(Thickness), textBlock.Margin);

            textBlock.SetPseudoClass(":pointerover", true);
            Assert.Equal(new Thickness(16, 0, 0, 0), textBlock.Margin);
            Assert.Contains(":pointerover", util.ResolveSelector().ToString());
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Bug 2: <c>GhostPropertyCombiner</c> drops the container query a breakpoint variant resolved.
    /// <c>sm:ml-4</c> must stay scoped to a <c>min-width:640</c> container query; today it applies at every width.
    /// </summary>
    [AvaloniaFact]
    public void BreakpointGhostMargin_KeepsItsContainerQuery()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"sm:ml-4\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var util = Assert.Single(utils);
        Assert.NotNull(util.ContainerQueryData);

        var query = util.ResolveContainerQuery();
        Assert.NotNull(query);
        Assert.Equal("min-width:640", query!.ToString());
    }

    /// <summary>
    /// Bug 3: a base ghost token and a hover variant of the same side are merged into one unconditional compound
    /// style by <c>GhostPropertyCombiner</c>, and the variant's value wins inside that single style. The two must
    /// remain separate states: (8,0,0,0) at rest, (16,0,0,0) while hovered.
    /// </summary>
    [AvaloniaFact]
    public void BaseAndHoverGhostMargins_StaySeparateStates()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"ml-2 hover:ml-4\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var textBlock = new TextBlock { Classes = { "ml-2", "hover:ml-4" } };
        var window = utils.ShowWithStyles(textBlock);
        try
        {
            Assert.Equal(new Thickness(8, 0, 0, 0), textBlock.Margin);

            textBlock.SetPseudoClass(":pointerover", true);
            Assert.Equal(new Thickness(16, 0, 0, 0), textBlock.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Bug 4: <c>GhostPropertyCombiner</c> only combines tokens found on the same source line. The same element's
    /// <c>ml-1</c> and <c>mr-2</c> split across two lines must still merge into one Margin of (4,0,8,0); today two
    /// styles each set the whole Margin, so the later (0,0,8,0) wins.
    /// </summary>
    [AvaloniaFact]
    public void GhostSides_SplitAcrossSourceLines_CombineIntoOneMargin()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"ml-1\n  mr-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var textBlock = new TextBlock { Classes = { "ml-1", "mr-2" } };
        var window = utils.ShowWithStyles(textBlock);
        try
        {
            Assert.Equal(new Thickness(4, 0, 8, 0), textBlock.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Bug 5: overlapping ghost groups are emitted as independent compound styles, so a shorter group seen later
    /// overwrites slots the longer group had already set. An element carrying ml-1, mr-2 and mt-3 must end up with
    /// (4,12,8,0); today the later <c>.ml-1.mr-2</c> style resets Margin to (4,0,8,0).
    /// </summary>
    [AvaloniaFact]
    public void OverlappingGhostGroups_DoNotOverwriteEachOthersSides()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"ml-1 mr-2 mt-3\"\nClasses=\"ml-1 mr-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var textBlock = new TextBlock { Classes = { "ml-1", "mr-2", "mt-3" } };
        var window = utils.ShowWithStyles(textBlock);
        try
        {
            Assert.Equal(new Thickness(4, 12, 8, 0), textBlock.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Bug 6: <c>PaddingRule</c>'s uniform branch targets <c>Decorator.PaddingProperty</c> with no explicit target
    /// type, so its selector is <c>Is&lt;Decorator&gt;</c> and never matches a <c>TemplatedControl</c> like
    /// <c>Button</c> (same <c>AddOwner</c> situation <c>bg-*</c> solves with explicit target types). <c>p-4</c> on a
    /// Button must yield Padding (16,16,16,16); today it stays 0.
    /// </summary>
    [AvaloniaFact]
    public void PaddingOnButton_AppliesToTemplatedControl()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("Classes=\"p-4\"", new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" });

        var button = new Button { Classes = { "p-4" } };
        var window = utils.ShowWithStyles(button);
        try
        {
            Assert.Equal(new Thickness(16), button.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Guard for bug 6: the existing <c>Is&lt;Decorator&gt;</c> target must keep working — a <c>Border</c> with
    /// <c>p-4</c> still gets Padding (16,16,16,16).
    /// </summary>
    [AvaloniaFact]
    public void PaddingOnBorder_StillApplies()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("Classes=\"p-4\"", new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" });

        var border = new Border { Classes = { "p-4" } };
        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(new Thickness(16), border.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Bug 7: <c>AtomicCli</c> resolves each source file separately and concatenates the results, so a token used
    /// in two files is emitted twice. The same <c>p-4</c> class in two sources must produce exactly one style.
    /// </summary>
    [AvaloniaFact]
    public void SameTokenInTwoSourceFiles_IsEmittedOnce()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();

        var directory = Path.Combine(Path.GetTempPath(), $"enx-atomic-known-bugs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var first = Path.Combine(directory, "First.axaml");
            var second = Path.Combine(directory, "Second.axaml");
            var output = Path.Combine(directory, "AtomicStyles.g.cs");
            File.WriteAllText(first, "<TextBlock Classes=\"p-4\" />");
            File.WriteAllText(second, "<Border Classes=\"p-4\" />");

            var exitCode = AtomicCli.Run(["--output", output, first, second], configuration);
            Assert.Equal(0, exitCode);

            var emitted = File.ReadAllText(output);
            Assert.Equal(1, CountOccurrences(emitted, "new Style("));
            Assert.Equal(1, CountOccurrences(emitted, ".Class(\"p-4\")"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var index = haystack.IndexOf(needle, StringComparison.Ordinal); index >= 0; )
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
