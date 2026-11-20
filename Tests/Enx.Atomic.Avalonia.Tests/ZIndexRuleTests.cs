using Avalonia;
using Avalonia.Controls;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// <c>z-*</c>: <see cref="Visual.ZIndexProperty"/> is an <c>int</c>, so a token must box an <c>int</c> and a
/// non-integral value is a miss rather than a rounded value. <c>z-auto</c> is deliberately unsupported (an
/// <c>int</c> cannot express CSS's "as if unset").
/// </summary>
public class ZIndexRuleTests
{
    [AvaloniaTheory]
    [InlineData("z-10", 10)]
    [InlineData("z10", 10)]
    [InlineData("z-0", 0)]
    [InlineData("z-[5]", 5)]
    [InlineData("z-[-3]", -3)]
    public void Tokens_ResolveToIntZIndex(string token, int expected)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        var setter = Assert.Single(util.Body);
        Assert.Equal(Visual.ZIndexProperty, setter.Property);
        Assert.Equal(expected, Assert.IsType<int>(setter.Value));
    }

    [AvaloniaTheory]
    [InlineData("z-auto")]
    [InlineData("z-1.5")]
    [InlineData("z-[auto]")]
    [InlineData("z-[1.5]")]
    [InlineData("z-")]
    public void UnrepresentableValues_DoNotMatch(string token)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        Assert.Empty(generator.ParseToken(token));
    }

    [AvaloniaFact]
    public void ZIndex_AppliesToAShownControl()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("z-10", new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "z-10" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(10, border.ZIndex);
        }
        finally
        {
            window.Close();
        }
    }
}
