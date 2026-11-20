using Avalonia;
using Avalonia.Controls;
using Enx.Atomic.Avalonia.Composites;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

public class SpacingRuleTests
{
    [AvaloniaTheory]
    [InlineData("m-4", 16)]
    [InlineData("-m-4", -16)]
    public void Margin_UniformVariant_WritesAllFourSides(string token, double uniform)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        // Uniform margin is the composite host's four sides, not Layoutable.MarginProperty: writing the real
        // property from a class-selected style would replace any other margin utility's whole Thickness.
        Assert.Equal(4, util.Body.Length);
        Assert.All(util.Body, setter => Assert.Equal(uniform, Assert.IsType<double>(setter.Value)));
        Assert.Equal(
            [
                MarginSides.MarginLeftProperty,
                MarginSides.MarginTopProperty,
                MarginSides.MarginRightProperty,
                MarginSides.MarginBottomProperty,
            ],
            util.Body.Select(setter => setter.Property)
        );
    }

    [AvaloniaFact]
    public void MarginAxis_TargetsBothSidesForThatAxis()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("mx-8"));

        var properties = util.Body.Select(setter => setter.Property).ToArray();
        Assert.Equal(2, properties.Length);
        Assert.Contains(MarginSides.MarginLeftProperty, properties);
        Assert.Contains(MarginSides.MarginRightProperty, properties);
        Assert.All(util.Body, setter => Assert.Equal(32d, Assert.IsType<double>(setter.Value)));
    }

    [AvaloniaFact]
    public void Margin_UnknownScaleKey_FallsBackToRemTimesFactor()
    {
        // "13" isn't in the default spacing scale, so it's treated as a bare rem number: 13 * 16px.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("m-13"));

        Assert.Equal(4, util.Body.Length);
        Assert.All(util.Body, setter => Assert.Equal(208d, Assert.IsType<double>(setter.Value)));
    }

    [AvaloniaFact]
    public void MarginSide_TargetsItsOwnSideProperty()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("ml-4"));

        var setter = Assert.Single(util.Body);
        Assert.Equal(MarginSides.MarginLeftProperty, setter.Property);
        Assert.Equal(16d, Assert.IsType<double>(setter.Value));
    }

    [AvaloniaTheory]
    [InlineData("m-4 mx-2")]
    [InlineData("mx-2 m-4")]
    public void MarginAxis_OverridesUniform_WhateverTheSourceOrder(string classes)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var utils = generator.Generate(
            $"Classes=\"{classes}\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var textBlock = new TextBlock { Classes = { "m-4", "mx-2" } };
        var window = utils.ShowWithStyles(textBlock);
        try
        {
            // UnoCSS's rule order: the axis entry outranks the uniform one, so left/right are 8 and top/bottom 16.
            Assert.Equal(new Thickness(8, 16, 8, 16), textBlock.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("mx-2 ml-1")]
    [InlineData("ml-1 mx-2")]
    public void MarginSide_OverridesItsAxis_WhateverTheSourceOrder(string classes)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var utils = generator.Generate(
            $"Classes=\"{classes}\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var textBlock = new TextBlock { Classes = { "ml-1", "mx-2" } };
        var window = utils.ShowWithStyles(textBlock);
        try
        {
            // The side entry is last in UnoCSS's order, so left is 4 while right keeps the axis value of 8.
            Assert.Equal(new Thickness(4, 0, 8, 0), textBlock.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Gap_SetsSpacingOnStackPanelAndSpacingOnGridBothAxes()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("gap-4");

        Assert.Equal(3, results.Length);
        Assert.All(results, util => Assert.All(util.Body, setter => Assert.Equal(16.0, setter.Value)));
    }
}
