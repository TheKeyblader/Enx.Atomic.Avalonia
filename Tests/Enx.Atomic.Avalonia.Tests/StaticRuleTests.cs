using Avalonia;
using Avalonia.Input;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Tests;

public class StaticRuleTests
{
    [AvaloniaFact]
    public void Hidden_SetsIsVisibleFalse()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("hidden");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Visual.IsVisibleProperty, setter.Property);
        Assert.Equal(false, setter.Value);
    }

    [AvaloniaFact]
    public void CursorPointer_SetsHandCursor()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("cursor-pointer");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(InputElement.CursorProperty, setter.Property);
        Assert.Equal("Hand", Assert.IsType<Cursor>(setter.Value).ToString());
    }

    [AvaloniaFact]
    public void FlexRow_AppliesToEveryOrientedPanelType()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("flex-row");

        Assert.Equal(5, results.Length);
        Assert.All(
            results,
            util =>
            {
                var setter = Assert.Single(util.Body);
                Assert.Equal(Orientation.Horizontal, setter.Value);
            }
        );
    }

    [AvaloniaFact]
    public void UnknownToken_ResolvesToNothing()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("not-a-real-class");

        Assert.Empty(results);
    }
}
