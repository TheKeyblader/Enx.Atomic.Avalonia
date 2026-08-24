namespace Enx.Atomic.Avalonia.Tests;

public class VariantTests
{
    [AvaloniaFact]
    public void Hover_AppendsPointerOverPseudoClassToSelector()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("hover:bg-red-500");

        // Three utils: BackgroundColorRule emits a Border.BackgroundProperty, a TemplatedControl.BackgroundProperty,
        // and a Panel.BackgroundProperty entry, since these are the exact same shared AvaloniaProperty
        // (TemplatedControl/Panel each AddOwner(Border.BackgroundProperty)) but need separate selectors — Border,
        // TemplatedControl, and Panel aren't related types, so one selector alone can't match all three. See
        // StyleValue.TargetType.
        Assert.Equal(3, results.Length);
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
        // Like hover:bg-red-500, this resolves to three utils (Border.BackgroundProperty,
        // TemplatedControl.BackgroundProperty, and Panel.BackgroundProperty) — each still needs the
        // dark-mode selector condition.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("dark:bg-red-500");

        Assert.Equal(3, results.Length);
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
