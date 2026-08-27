using Avalonia;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Tests;

public class SpacingRuleTests
{
    [AvaloniaTheory]
    [InlineData("m-4", 16)]
    [InlineData("-m-4", -16)]
    public void Margin_UniformVariant_SetsTheRealMarginProperty(string token, double uniform)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken(token);

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Layoutable.MarginProperty, setter.Property);
        Assert.Equal(new Thickness(uniform), setter.Value);
    }

    [AvaloniaFact]
    public void MarginAxis_TargetsBothGhostPropertiesForThatAxis()
    {
        // Like the single-side branches, the x/y axis branch doesn't zero the other sides on the real
        // property — it targets two ghost properties, left for the combiner to assemble.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("mx-8");

        // Both ghost properties share the same (SpecialProperties) owner type, so they land in one util.
        var util = Assert.Single(results);
        Assert.Equal(2, util.Body.Length);
        var properties = util.Body.Select(s => s.Property).ToArray();
        Assert.Contains(SpecialProperties.MarginLeftProperty, properties);
        Assert.Contains(SpecialProperties.MarginRightProperty, properties);
        Assert.All(util.Body, s => Assert.Equal(32f, s.Value));
    }

    [AvaloniaFact]
    public void Margin_UnknownScaleKey_FallsBackToRemTimesFactor()
    {
        // "13" isn't in the default spacing scale, so it's treated as a bare rem number: 13 * 16px.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("m-13");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(new Thickness(208), setter.Value);
    }

    [AvaloniaFact]
    public void Gap_SetsSpacingOnStackPanelAndSpacingOnGridBothAxes()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("gap-4");

        Assert.Equal(3, results.Length);
        Assert.All(results, util => Assert.All(util.Body, setter => Assert.Equal(16.0, setter.Value)));
    }

    [AvaloniaFact]
    public void MarginSide_TargetsGhostPropertyNotTheRealProperty()
    {
        // The per-side branch doesn't zero the other sides on the real property anymore — it targets a
        // ghost property, which the combiner (tested separately) is what turns into a real Margin value.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("ml-4");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(SpecialProperties.MarginLeftProperty, setter.Property);
        Assert.Equal(16f, setter.Value);
    }
}
