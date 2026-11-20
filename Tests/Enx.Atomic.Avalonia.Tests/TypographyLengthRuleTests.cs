using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// <c>leading-*</c>/<c>lh-*</c>/<c>line-height-*</c> and <c>tracking-*</c>: Avalonia's <c>LineHeight</c> and
/// <c>LetterSpacing</c> are absolute pixels, so only the arbitrary-px and bare-number forms resolve — upstream's
/// unitless-multiplier and <c>em</c> scale entries are deliberately not ported (they would need the element's own
/// resolved font size, which a style cannot read).
/// </summary>
public class TypographyLengthRuleTests
{
    [AvaloniaTheory]
    [InlineData("leading-4", 16)]
    [InlineData("lh-2", 8)]
    [InlineData("line-height-1", 4)]
    [InlineData("leading-[24px]", 24)]
    public void LineHeight_ResolvesAbsoluteLengths(string token, double expected)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        var setter = Assert.Single(util.Body);
        Assert.Equal(TextBlock.LineHeightProperty, setter.Property);
        Assert.Equal(expected, Assert.IsType<double>(setter.Value));
    }

    [AvaloniaTheory]
    [InlineData("leading-none")]
    [InlineData("leading-tight")]
    [InlineData("leading-normal")]
    public void LineHeight_MultipliersDoNotMatch(string token)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        Assert.Empty(generator.ParseToken(token));
    }

    /// <summary>
    /// An arbitrary value is always a pixel literal in this preset, so <c>leading-[1.5]</c> is 1.5px — not
    /// upstream's 1.5× multiplier. That is the documented bracket convention, not an accident of this rule.
    /// </summary>
    [AvaloniaFact]
    public void LineHeight_ArbitraryBareNumberIsPixels()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("leading-[1.5]"));

        Assert.Equal(1.5d, Assert.IsType<double>(Assert.Single(util.Body).Value));
    }

    [AvaloniaTheory]
    [InlineData("tracking-2", 8)]
    [InlineData("tracking-[2px]", 2)]
    [InlineData("tracking-[0]", 0)]
    public void LetterSpacing_ResolvesAbsoluteLengths(string token, double expected)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        var setter = Assert.Single(util.Body);
        Assert.Equal(TextElement.LetterSpacingProperty, setter.Property);
        Assert.Equal(expected, Assert.IsType<double>(setter.Value));
    }

    [AvaloniaTheory]
    [InlineData("tracking-tight")]
    [InlineData("tracking-normal")]
    [InlineData("tracking-wide")]
    [InlineData("tracking-[0.05em]")]
    public void LetterSpacing_EmScaleEntriesDoNotMatch(string token)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        Assert.Empty(generator.ParseToken(token));
    }

    [AvaloniaFact]
    public void BothProperties_ApplyToAShownTextBlock()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("leading-[30px] tracking-[3px]", new AtomicGenerator<MiniTheme>.Options());
        var text = new TextBlock { Text = "Text", Classes = { "leading-[30px]", "tracking-[3px]" } };

        var window = utils.ShowWithStyles(text);
        try
        {
            Assert.Equal(30, text.LineHeight);
            Assert.Equal(3, text.LetterSpacing);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Two line-height tokens set the same property with no relative rank; the engine's deterministic ordering (not
    /// the source order) decides, so both spellings end up with the same value.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("leading-4 leading-8")]
    [InlineData("leading-8 leading-4")]
    public void TwoLineHeights_ResolveTheSameWhateverTheSourceOrder(string source)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options());
        var text = new TextBlock { Classes = { "leading-4", "leading-8" } };

        var window = utils.ShowWithStyles(text);
        try
        {
            Assert.Equal(32, text.LineHeight);
        }
        finally
        {
            window.Close();
        }
    }
}
