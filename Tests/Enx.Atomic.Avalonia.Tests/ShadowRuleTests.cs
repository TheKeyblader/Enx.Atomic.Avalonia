using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// <c>shadow-*</c> is the size half of UnoCSS's shadow rule only: it maps the ported <c>boxShadow</c> scale onto
/// <see cref="Border.BoxShadowProperty"/> and <see cref="ContentPresenter.BoxShadowProperty"/>. These tests assert
/// both the emitted setters (multi-layer, inset, no-shadow) and the value a shown control actually ends up with.
/// </summary>
public class ShadowRuleTests
{
    [AvaloniaFact]
    public void Shadow_WithoutKey_UsesTheDefaultScaleEntry()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var utils = generator.ParseToken("shadow");

        // One util per target type: Border and ContentPresenter share the property instance, not the selector.
        Assert.Equal(2, utils.Length);
        Assert.Equal(
            [Border.BoxShadowProperty, ContentPresenter.BoxShadowProperty],
            utils.Select(util => Assert.Single(util.Body).Property)
        );

        var shadows = Assert.IsType<BoxShadows>(Assert.Single(utils[0].Body).Value);
        Assert.Equal(2, shadows.Count);
        Assert.Equal(3, shadows[0].Blur);
    }

    [AvaloniaFact]
    public void ShadowMd_EmitsBothLayersWithTheirSpread()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("shadow-md")[0].Body);

        var shadows = Assert.IsType<BoxShadows>(util.Value);
        Assert.Equal(2, shadows.Count);

        Assert.Equal(0, shadows[0].OffsetX);
        Assert.Equal(4, shadows[0].OffsetY);
        Assert.Equal(6, shadows[0].Blur);
        Assert.Equal(-1, shadows[0].Spread);
        Assert.False(shadows[0].IsInset);
        Assert.Equal(Color.FromArgb(0x1A, 0, 0, 0), shadows[0].Color);

        Assert.Equal(2, shadows[1].OffsetY);
        Assert.Equal(4, shadows[1].Blur);
        Assert.Equal(-2, shadows[1].Spread);
    }

    [AvaloniaFact]
    public void ShadowInner_IsTheOnlyInsetEntry()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("shadow-inner")[0].Body);

        var shadows = Assert.IsType<BoxShadows>(util.Value);
        Assert.Equal(1, shadows.Count);
        var layer = shadows[0];
        Assert.True(layer.IsInset);
        Assert.Equal(2, layer.OffsetY);
        Assert.Equal(4, layer.Blur);
        Assert.Equal(0, layer.Spread);
        Assert.Equal(Color.FromArgb(0x0D, 0, 0, 0), layer.Color);
    }

    [AvaloniaFact]
    public void ShadowNone_IsAnEmptyBoxShadows()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("shadow-none")[0].Body);

        var shadows = Assert.IsType<BoxShadows>(util.Value);
        Assert.Equal(0, shadows.Count);
    }

    /// <summary>Shadow colors, the <c>shadow-inset</c> modifier and arbitrary values are deliberately not ported — an unknown key is a miss, not a colour lookup.</summary>
    [AvaloniaTheory]
    [InlineData("shadow-red-500")]
    [InlineData("shadow-inset")]
    [InlineData("shadow-opacity-50")]
    [InlineData("shadow-[0_0_1px_red]")]
    public void UnportedShadowForms_ResolveToNothing(string token)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        Assert.Empty(generator.ParseToken(token));
    }

    [AvaloniaFact]
    public void ShadowMd_AppliesToABorder()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("shadow-md", new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "shadow-md" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(2, border.BoxShadow.Count);
            Assert.Equal(4, border.BoxShadow[0].OffsetY);
            Assert.Equal(-1, border.BoxShadow[0].Spread);
            Assert.Equal(Color.FromArgb(0x1A, 0, 0, 0), border.BoxShadow[0].Color);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// <see cref="ContentPresenter.BoxShadowProperty"/> is <c>Border.BoxShadowProperty.AddOwner</c>'d, so its
    /// <c>OwnerType</c> reports <see cref="Border"/>; the rule's explicit target type is what makes this style
    /// match a <see cref="ContentPresenter"/> at all.
    /// </summary>
    [AvaloniaFact]
    public void ShadowMd_AppliesToAContentPresenter()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("shadow-md", new AtomicGenerator<MiniTheme>.Options());
        var presenter = new ContentPresenter { Classes = { "shadow-md" } };

        var window = utils.ShowWithStyles(presenter);
        try
        {
            Assert.Equal(2, presenter.BoxShadow.Count);
            Assert.Equal(4, presenter.BoxShadow[0].OffsetY);
        }
        finally
        {
            window.Close();
        }
    }
}
