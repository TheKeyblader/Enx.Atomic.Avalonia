using Avalonia;
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Tests;

public class BorderRuleTests
{
    [AvaloniaFact]
    public void BorderWidth_UniformVariant_SetsTheRealBorderThicknessProperty()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("border-2");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.BorderThicknessProperty, setter.Property);
        Assert.Equal(new Thickness(2), setter.Value);
    }

    public static TheoryData<string, AvaloniaProperty> BorderWidthSideCases =>
        new()
        {
            { "border-t-2", SpecialProperties.BorderThicknessTopProperty },
            { "border-r-2", SpecialProperties.BorderThicknessRightProperty },
            { "border-b-2", SpecialProperties.BorderThicknessBottomProperty },
            { "border-l-2", SpecialProperties.BorderThicknessLeftProperty },
        };

    [AvaloniaTheory]
    [MemberData(nameof(BorderWidthSideCases))]
    public void BorderWidthSide_TargetsGhostPropertyNotTheRealProperty(string token, AvaloniaProperty property)
    {
        // Like the per-side margin/padding branches, this doesn't zero the other sides on the real
        // property — it targets a ghost property, which the combiner assembles into a real value.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken(token);

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(property, setter.Property);
        Assert.Equal(2f, setter.Value);
    }

    [AvaloniaFact]
    public void BorderWidthSide_NoValue_FallsBackToDefaultScaleKey()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("border-t");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(SpecialProperties.BorderThicknessTopProperty, setter.Property);
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
