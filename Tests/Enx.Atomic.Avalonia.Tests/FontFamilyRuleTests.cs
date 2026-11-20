using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// <c>font-{family}</c>: the value is an <see cref="IFontFamilyPart.FontFamilies"/> entry. The <c>font-</c> prefix is
/// shared with the static weight rules, so the prefix-selection cases live in <c>AmbiguousPrefixTests</c>.
/// </summary>
public class FontFamilyRuleTests
{
    [AvaloniaFact]
    public void FontSans_ResolvesToThePlatformDefault()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("font-sans"));

        var setter = Assert.Single(util.Body);
        Assert.Equal(TextElement.FontFamilyProperty, setter.Property);
        Assert.Equal(FontFamily.Default, Assert.IsType<FontFamily>(setter.Value));
    }

    [AvaloniaTheory]
    [InlineData("font-serif", "serif")]
    [InlineData("font-mono", "mono")]
    public void ScaledFamilies_ResolveFromTheTheme(string token, string key)
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        var setter = Assert.Single(util.Body);
        Assert.Equal(TextElement.FontFamilyProperty, setter.Property);
        // The rule stores the theme's own instance (a Literal), not a copy.
        Assert.Same(configuration.Theme.FontFamilies[key], setter.Value);
    }

    [AvaloniaTheory]
    [InlineData("font-unknown")]
    [InlineData("font-[Arial]")]
    [InlineData("font-")]
    public void ValuesOutsideTheScale_DoNotMatch(string token)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        Assert.Empty(generator.ParseToken(token));
    }

    [AvaloniaFact]
    public void FontFamily_AppliesToAShownTextBlock()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("font-mono", new AtomicGenerator<MiniTheme>.Options());
        var text = new TextBlock { Text = "Text", Classes = { "font-mono" } };

        var window = utils.ShowWithStyles(text);
        try
        {
            Assert.Equal(configuration.Theme.FontFamilies["mono"], text.FontFamily);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Two families set the same property with no relative rank; the engine's deterministic ordering (not the source
    /// order) decides, so both spellings end up with the same value.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("font-sans font-mono")]
    [InlineData("font-mono font-sans")]
    public void TwoFamilies_ResolveTheSameWhateverTheSourceOrder(string source)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options());
        var text = new TextBlock { Classes = { "font-sans", "font-mono" } };

        var window = utils.ShowWithStyles(text);
        try
        {
            Assert.Equal(FontFamily.Default, text.FontFamily);
        }
        finally
        {
            window.Close();
        }
    }
}
