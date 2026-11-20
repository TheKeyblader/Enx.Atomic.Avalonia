using Avalonia;
using Avalonia.Controls;
using Enx.Atomic.Avalonia.Composites;

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
}
