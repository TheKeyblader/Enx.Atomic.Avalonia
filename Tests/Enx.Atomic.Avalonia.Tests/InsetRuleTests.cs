using Avalonia;
using Avalonia.Controls;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// <c>inset-*</c>/<c>top-*</c>/<c>right-*</c>/<c>bottom-*</c>/<c>left-*</c>: <see cref="Canvas"/>'s four attached
/// placement properties, resolved from the spacing scale, with a leading <c>-</c> negating the value. The rank of the
/// uniform/axis/side forms is asserted in both source orders.
/// </summary>
public class InsetRuleTests
{
    public static TheoryData<string, AvaloniaProperty, double> SingleSideCases =>
        new()
        {
            { "top-4", Canvas.TopProperty, 16 },
            { "right-4", Canvas.RightProperty, 16 },
            { "bottom-4", Canvas.BottomProperty, 16 },
            { "left-4", Canvas.LeftProperty, 16 },
            { "inset-l-2", Canvas.LeftProperty, 8 },
            { "-top-4", Canvas.TopProperty, -16 },
            { "top-[12px]", Canvas.TopProperty, 12 },
            { "top-[-10px]", Canvas.TopProperty, -10 },
        };

    [AvaloniaTheory]
    [MemberData(nameof(SingleSideCases))]
    public void SingleSide_TargetsItsOwnProperty(string token, AvaloniaProperty property, double expected)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        var setter = Assert.Single(util.Body);
        Assert.Equal(property, setter.Property);
        Assert.Equal(expected, Assert.IsType<double>(setter.Value));
    }

    [AvaloniaFact]
    public void UniformInset_WritesAllFourSides()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("inset-4"));

        Assert.Equal(
            [Canvas.LeftProperty, Canvas.TopProperty, Canvas.RightProperty, Canvas.BottomProperty],
            util.Body.Select(setter => setter.Property)
        );
        Assert.All(util.Body, setter => Assert.Equal(16d, Assert.IsType<double>(setter.Value)));
    }

    [AvaloniaFact]
    public void AxisInset_WritesBothSidesOfThatAxis()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var horizontal = Assert.Single(generator.ParseToken("inset-x-2"));
        Assert.Equal([Canvas.LeftProperty, Canvas.RightProperty], horizontal.Body.Select(setter => setter.Property));

        var vertical = Assert.Single(generator.ParseToken("-inset-y-2"));
        Assert.Equal([Canvas.TopProperty, Canvas.BottomProperty], vertical.Body.Select(setter => setter.Property));
        Assert.All(vertical.Body, setter => Assert.Equal(-8d, Assert.IsType<double>(setter.Value)));
    }

    [AvaloniaTheory]
    [InlineData("top-x-4")]
    [InlineData("inset-auto")]
    [InlineData("top-50%")]
    public void UnsupportedForms_DoNotMatch(string token)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        Assert.Empty(generator.ParseToken(token));
    }

    [AvaloniaFact]
    public void Insets_ApplyToAShownCanvasChild()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("top-4 left-2", new AtomicGenerator<MiniTheme>.Options());
        var child = new Border
        {
            Width = 20,
            Height = 20,
            Classes = { "top-4", "left-2" },
        };
        var canvas = new Canvas();
        canvas.Children.Add(child);

        var window = utils.ShowWithStyles(canvas);
        try
        {
            Assert.Equal(16, Canvas.GetTop(child));
            Assert.Equal(8, Canvas.GetLeft(child));
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// The side wins over the axis, and the axis over the uniform <c>inset-*</c>, whatever the source order — the
    /// rule's rank mirrors UnoCSS's increasing rule entries.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("inset-0 inset-y-2 top-4")]
    [InlineData("top-4 inset-y-2 inset-0")]
    public void Ranks_RunFromUniformToAxisToSide(string source)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options());
        var child = new Border { Classes = { "inset-0", "inset-y-2", "top-4" } };
        var canvas = new Canvas();
        canvas.Children.Add(child);

        var window = utils.ShowWithStyles(canvas);
        try
        {
            Assert.Equal(16, Canvas.GetTop(child));
            Assert.Equal(8, Canvas.GetBottom(child));
            Assert.Equal(0, Canvas.GetLeft(child));
            Assert.Equal(0, Canvas.GetRight(child));
        }
        finally
        {
            window.Close();
        }
    }
}
