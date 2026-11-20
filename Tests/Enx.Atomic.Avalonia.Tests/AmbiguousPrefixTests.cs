using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Enx.Atomic.Avalonia.Composites;

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

        var util = Assert.Single(generator.ParseToken("border"));

        // Uniform width writes the composite host's four sides (see BorderWidthRule); the prefix decision this
        // test is about is that "border" resolved as a width at all, not as a color.
        Assert.Equal(4, util.Body.Length);
        Assert.All(util.Body, setter => Assert.Equal(1d, Assert.IsType<double>(setter.Value)));
        Assert.All(util.Body, setter => Assert.Equal(typeof(BorderThicknessSides), setter.Property!.OwnerType));
    }

    [AvaloniaFact]
    public void BorderNumber_ResolvesToWidthInPixels_NotColor()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("border-2"));

        Assert.Equal(4, util.Body.Length);
        Assert.All(util.Body, setter => Assert.Equal(2d, Assert.IsType<double>(setter.Value)));
        Assert.All(util.Body, setter => Assert.Equal(typeof(BorderThicknessSides), setter.Property!.OwnerType));
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
