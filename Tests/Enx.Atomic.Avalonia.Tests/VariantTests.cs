using Avalonia.Styling;

namespace Enx.Atomic.Avalonia.Tests;

public class VariantTests
{
    [AvaloniaFact]
    public void Hover_AppendsPointerOverPseudoClassToSelector()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("hover:bg-red-500");

        // Two utils: BackgroundColorRule emits both a Border.BackgroundProperty and a
        // TemplatedControl.BackgroundProperty entry, since those are the exact same shared AvaloniaProperty
        // (TemplatedControl.AddOwner(Border.BackgroundProperty)) but need separate selectors — Border and
        // TemplatedControl aren't related types, so one selector alone can't match both. See StyleValue.TargetType.
        Assert.Equal(2, results.Length);
        Assert.All(results, util => Assert.Contains(":pointerover", util.ResolveSelector().ToString()));
    }

    [AvaloniaFact]
    public void ChainedPseudoClasses_BothApply()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("hover:focus:underline");

        var util = Assert.Single(results);
        var selector = util.ResolveSelector().ToString();
        Assert.Contains(":pointerover", selector);
        Assert.Contains(":focus", selector);
    }

    [AvaloniaFact]
    public void Breakpoint_ProducesMinWidthContainerQuery()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("sm:hidden");

        var util = Assert.Single(results);
        var query = util.ResolveContainerQuery();
        Assert.NotNull(query);
        Assert.Equal("min-width:640", query!.ToString());
    }

    [AvaloniaFact]
    public void MaxBreakpoint_ProducesMaxWidthContainerQuery()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("max-sm:hidden");

        var util = Assert.Single(results);
        var query = util.ResolveContainerQuery();
        Assert.NotNull(query);
        Assert.Equal("max-width:640", query!.ToString());
    }

    [AvaloniaFact]
    public void Dark_AppendsActualThemeVariantPropertyEqualsToSelector()
    {
        // Like hover:bg-red-500, this resolves to two utils (Border.BackgroundProperty and
        // TemplatedControl.BackgroundProperty) — each still needs the dark-mode selector condition.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("dark:bg-red-500");

        Assert.Equal(2, results.Length);
        Assert.All(
            results,
            util => Assert.Contains("[ActualThemeVariant=Dark]", util.ResolveSelector().ToString())
        );
    }

    [AvaloniaFact]
    public void ChainedDarkAndPseudoClass_BothApply()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("dark:hover:underline");

        var util = Assert.Single(results);
        var selector = util.ResolveSelector().ToString();
        Assert.Contains("[ActualThemeVariant=Dark]", selector);
        Assert.Contains(":pointerover", selector);
    }
}
