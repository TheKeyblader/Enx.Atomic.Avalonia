using Avalonia;
using Avalonia.Controls;
using Enx.Atomic.Avalonia.Composites;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

public class BorderRuleTests
{
    [AvaloniaFact]
    public void BorderWidth_UniformVariant_WritesAllFourSides()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("border-2"));

        // Uniform border width is the host's four sides, not Border.BorderThicknessProperty: writing the real
        // property from a class-selected style would replace any other border-width utility's whole Thickness.
        Assert.Equal(4, util.Body.Length);
        Assert.All(util.Body, setter => Assert.Equal(2d, Assert.IsType<double>(setter.Value)));
        Assert.Equal(
            [
                BorderThicknessSides.BorderThicknessLeftProperty,
                BorderThicknessSides.BorderThicknessTopProperty,
                BorderThicknessSides.BorderThicknessRightProperty,
                BorderThicknessSides.BorderThicknessBottomProperty,
            ],
            util.Body.Select(setter => setter.Property)
        );
    }

    public static TheoryData<string, AvaloniaProperty> BorderWidthSideCases =>
        new()
        {
            { "border-t-2", BorderThicknessSides.BorderThicknessTopProperty },
            { "border-r-2", BorderThicknessSides.BorderThicknessRightProperty },
            { "border-b-2", BorderThicknessSides.BorderThicknessBottomProperty },
            { "border-l-2", BorderThicknessSides.BorderThicknessLeftProperty },
        };

    [AvaloniaTheory]
    [MemberData(nameof(BorderWidthSideCases))]
    public void BorderWidthSide_TargetsItsOwnSideProperty(string token, AvaloniaProperty property)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        var setter = Assert.Single(util.Body);
        Assert.Equal(property, setter.Property);
        Assert.Equal(2d, Assert.IsType<double>(setter.Value));
    }

    [AvaloniaFact]
    public void BorderWidthSide_NoValue_FallsBackToDefaultScaleKey()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("border-t"));

        var setter = Assert.Single(util.Body);
        Assert.Equal(BorderThicknessSides.BorderThicknessTopProperty, setter.Property);
        Assert.Equal(1d, Assert.IsType<double>(setter.Value));
    }

    [AvaloniaFact]
    public void BorderColor_StillMatchesWhenValueIsNotAWidthScaleKey()
    {
        // "border-" tokens that don't resolve as a line-width fall through to BorderColorRule — the
        // per-side regex change must not break that ambiguity resolution.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("border-red-500");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.BorderBrushProperty, setter.Property);
    }

    [AvaloniaFact]
    public void BorderWidthAxis_TargetsBothSidesForThatAxis()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("border-x-2"));

        Assert.Equal(
            [BorderThicknessSides.BorderThicknessLeftProperty, BorderThicknessSides.BorderThicknessRightProperty],
            util.Body.Select(setter => setter.Property)
        );
        Assert.All(util.Body, setter => Assert.Equal(2d, Assert.IsType<double>(setter.Value)));
    }

    [AvaloniaFact]
    public void BorderWidthHorizontalAxis_ResolvesAnArbitraryPixelValue()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("border-y-[3px]"));

        Assert.Equal(2, util.Body.Length);
        Assert.All(util.Body, setter => Assert.Equal(3d, Assert.IsType<double>(setter.Value)));
    }

    /// <summary>
    /// Uniform, axis and side widths all write the composite host's sides, so they compose into one thickness
    /// instead of replacing each other. The rule's rank — uniform &lt; axis &lt; side — decides which wins for a
    /// given side, whatever the source order (see ARCHITECTURE.md, "Emission order").
    /// </summary>
    [AvaloniaTheory]
    [InlineData("border-2 border-x-4 border-t-8")]
    [InlineData("border-t-8 border-x-4 border-2")]
    public void BorderWidth_RanksAxisAboveUniformAndSideAboveAxis(string source)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "border-2", "border-x-4", "border-t-8" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(new Thickness(4, 8, 4, 2), border.BorderThickness);
        }
        finally
        {
            window.Close();
        }
    }
}
