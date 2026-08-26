using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// Several rule families share a token prefix (<c>border-</c>, <c>text-</c>); the more specific rule must be
/// declared before the more generic one so it wins when its scale actually has the key, and falls through
/// otherwise. See <c>ThemeBuilderExtensions.AddMiniTheme</c>'s registration order.
/// </summary>
public class AmbiguousPrefixTests
{
    [AvaloniaFact]
    public void Border_Bare_UsesDefaultLineWidth()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("border");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.BorderThicknessProperty, setter.Property);
        Assert.Equal(new Thickness(1), setter.Value);
    }

    [AvaloniaFact]
    public void BorderNumber_ResolvesToWidthInPixels_NotColor()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("border-2");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.BorderThicknessProperty, setter.Property);
        Assert.Equal(new Thickness(2), setter.Value);
    }

    [AvaloniaFact]
    public void BorderColorName_ResolvesToColor_NotWidth()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("border-red-500");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.BorderBrushProperty, setter.Property);
        // border-* now resolves to a resource-based value (StyleValue.Resource) — see BackgroundColorRule's
        // sibling BorderColorRule and StyleValue.Resource.
        Assert.Equal("Colors[red-500]", Assert.IsType<DynamicResourceExtension>(setter.Value).ResourceKey);
    }

    [AvaloniaFact]
    public void TextScaleKey_ResolvesToFontSize_NotColor()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("text-sm");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(TextElement.FontSizeProperty, setter.Property);
        Assert.Equal(14d, setter.Value);
    }

    [AvaloniaFact]
    public void TextColorName_ResolvesToForeground_NotFontSize()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("text-red-500");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(TextElement.ForegroundProperty, setter.Property);
        Assert.Equal("Colors[red-500]", Assert.IsType<DynamicResourceExtension>(setter.Value).ResourceKey);
    }
}
