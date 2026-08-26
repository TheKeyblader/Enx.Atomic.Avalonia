using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>UnoCSS/Tailwind's bracket escape hatch (<c>bg-[#ff0000]</c>, <c>w-[123px]</c>) — bypasses the theme scale entirely, always resolving to a fixed <see cref="StyleValue.Literal{TValue}"/>.</summary>
public class ArbitraryValueTests
{
    [AvaloniaFact]
    public void ArbitraryColor_ResolvesToALiteralBrush_NotAResource()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("bg-[#ff0000]");

        Assert.Equal(3, results.Length);
        var borderUtil = Assert.Single(results, u => u.ResolveSelector().ToString().Contains(nameof(Border)));
        var setter = Assert.Single(borderUtil.Body);
        Assert.Equal(Color.Parse("#ff0000"), Assert.IsType<SolidColorBrush>(setter.Value).Color);
    }

    [AvaloniaFact]
    public void ArbitraryColor_DoesNotRegisterAResource()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        generator.ParseToken("bg-[#ff0000]");

        Assert.Empty(generator.ResolvedResources);
    }

    [AvaloniaFact]
    public void ArbitraryColor_ForegroundAndBorder_AlsoResolve()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var text = Assert.Single(generator.ParseToken("text-[#00ff00]"));
        var border = Assert.Single(generator.ParseToken("border-[#0000ff]"));

        Assert.Equal(Color.Parse("#00ff00"), Assert.IsType<SolidColorBrush>(Assert.Single(text.Body).Value).Color);
        Assert.Equal(Color.Parse("#0000ff"), Assert.IsType<SolidColorBrush>(Assert.Single(border.Body).Value).Color);
    }

    [AvaloniaFact]
    public void InvalidArbitraryColor_DoesNotMatch()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("bg-[not-a-color]");

        Assert.Empty(results);
    }

    [AvaloniaTheory]
    [InlineData("w-[123px]", 123d)]
    [InlineData("w-[123]", 123d)]
    public void ArbitraryWidth_TakenLiterallyAsPixels_NoRemConversion(string token, double expected)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken(token);

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Layoutable.WidthProperty, setter.Property);
        Assert.Equal(expected, setter.Value);
    }

    [AvaloniaFact]
    public void ArbitraryMargin_UniformSide_SetsThickness()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("m-[10px]");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Layoutable.MarginProperty, setter.Property);
        Assert.Equal(new Thickness(10), setter.Value);
    }

    [AvaloniaFact]
    public void ArbitraryRadius_SetsCornerRadius()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("rounded-[6px]");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.CornerRadiusProperty, setter.Property);
        Assert.Equal(new CornerRadius(6), setter.Value);
    }

    [AvaloniaFact]
    public void ArbitraryBorderWidth_ResolvesAsWidth_NotColor()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("border-[3px]");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.BorderThicknessProperty, setter.Property);
        Assert.Equal(new Thickness(3), setter.Value);
    }

    [AvaloniaFact]
    public void ArbitraryFontSize_SetsFontSize()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("text-[18px]");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(TextElement.FontSizeProperty, setter.Property);
        Assert.Equal(18d, setter.Value);
    }

    [AvaloniaTheory]
    [InlineData("w-[abc]")]
    [InlineData("w-[50%]")]
    public void ArbitraryWidth_UnsupportedUnit_DoesNotMatch(string token)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken(token);

        Assert.Empty(results);
    }

    [AvaloniaFact]
    public void NamedScaleValue_StillResolvesNormally_AlongsideArbitrarySupport()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("p-4");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Decorator.PaddingProperty, setter.Property);
    }
}
