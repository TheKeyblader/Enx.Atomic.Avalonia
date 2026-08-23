using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// Several rule families share a token prefix (<c>border-</c>, <c>text-</c>); the more specific rule must be
/// declared before the more generic one so it wins when its scale actually has the key, and falls through
/// otherwise. See <c>ThemeBuilderExtensions.AddMiniTheme</c>'s registration order.
/// </summary>
public class AmbiguousPrefixTests
{
    [Fact]
    public void Border_Bare_UsesDefaultLineWidth()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("border");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.BorderThicknessProperty, setter.Property);
        Assert.Equal(new Thickness(1), setter.Value);
    }

    [Fact]
    public void BorderNumber_ResolvesToWidthInPixels_NotColor()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("border-2");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.BorderThicknessProperty, setter.Property);
        Assert.Equal(new Thickness(2), setter.Value);
    }

    [Fact]
    public void BorderColorName_ResolvesToColor_NotWidth()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("border-red-500");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.BorderBrushProperty, setter.Property);
        Assert.Equal(Color.Parse("#ef4444"), Assert.IsType<SolidColorBrush>(setter.Value).Color);
    }

    [Fact]
    public void TextScaleKey_ResolvesToFontSize_NotColor()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("text-sm");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(TextElement.FontSizeProperty, setter.Property);
        Assert.Equal(14d, setter.Value);
    }

    [Fact]
    public void TextColorName_ResolvesToForeground_NotFontSize()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("text-red-500");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(TextElement.ForegroundProperty, setter.Property);
        Assert.Equal(Color.Parse("#ef4444"), Assert.IsType<SolidColorBrush>(setter.Value).Color);
    }
}
