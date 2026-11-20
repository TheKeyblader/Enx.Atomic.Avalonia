using Avalonia;
using Avalonia.Controls;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// <c>opacity-*</c>/<c>op-*</c>: a bare token is a percentage (upstream's <c>h.bracket.percent</c>), an arbitrary
/// value is taken as written. The effective value is asserted on a shown control, since the setter's boxed
/// <c>double</c> and the property Avalonia applies must agree.
/// </summary>
public class OpacityRuleTests
{
    [AvaloniaTheory]
    [InlineData("opacity-50", 0.5)]
    [InlineData("opacity-100", 1)]
    [InlineData("opacity-0.5", 0.005)]
    [InlineData("op-25", 0.25)]
    public void BareTokens_ArePercentages(string token, double expected)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        var setter = Assert.Single(util.Body);
        Assert.Equal(Visual.OpacityProperty, setter.Property);
        Assert.Equal(expected, Assert.IsType<double>(setter.Value));
    }

    [AvaloniaTheory]
    [InlineData("opacity-[0.35]", 0.35)]
    [InlineData("opacity-[50%]", 0.5)]
    public void ArbitraryValues_AreTakenAsWritten(string token, double expected)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        var setter = Assert.Single(util.Body);
        Assert.Equal(Visual.OpacityProperty, setter.Property);
        Assert.Equal(expected, Assert.IsType<double>(setter.Value));
    }

    [AvaloniaTheory]
    [InlineData("opacity-foo")]
    [InlineData("op-")]
    [InlineData("opacity-[nonsense]")]
    public void UnparseableValues_DoNotMatch(string token)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        Assert.Empty(generator.ParseToken(token));
    }

    [AvaloniaFact]
    public void Opacity_AppliesToAShownControl()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("opacity-50", new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "opacity-50" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(0.5, border.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Two opacity tokens set the same property and have no relative rank; the engine's deterministic ordering
    /// (not the source order) decides, so both spellings end up with the same value.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("opacity-50 opacity-100")]
    [InlineData("opacity-100 opacity-50")]
    public void TwoTokens_ResolveTheSameWhateverTheSourceOrder(string source)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "opacity-50", "opacity-100" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(0.5, border.Opacity);
        }
        finally
        {
            window.Close();
        }
    }
}
