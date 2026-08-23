using Avalonia.Styling;

namespace Enx.Atomic.Avalonia.Tests;

public class VariantTests
{
    [AvaloniaFact]
    public void Hover_AppendsPointerOverPseudoClassToSelector()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("hover:bg-red-500");

        var util = Assert.Single(results);
        Assert.Contains(":pointerover", util.ResolveSelector().ToString());
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
}
