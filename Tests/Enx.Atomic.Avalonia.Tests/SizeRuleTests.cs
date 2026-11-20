using Avalonia.Controls;
using Avalonia.Layout;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// <c>w-full</c>/<c>h-full</c>/<c>w-auto</c>/<c>h-auto</c> have no percentage equivalent in Avalonia: they clear
/// the length and, for the <c>full</c> forms, stretch the control across the space its parent offers. These tests
/// assert the effective property values on a shown control, plus the rule's deterministic rank against a scale
/// value in both source orders.
/// </summary>
public class SizeRuleTests
{
    [AvaloniaFact]
    public void WidthFull_ClearsWidthAndStretchesHorizontally()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("w-full", new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "w-full" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.True(double.IsNaN(border.Width));
            Assert.Equal(HorizontalAlignment.Stretch, border.HorizontalAlignment);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HeightFull_ClearsHeightAndStretchesVertically()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("h-full", new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "h-full" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.True(double.IsNaN(border.Height));
            Assert.Equal(VerticalAlignment.Stretch, border.VerticalAlignment);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void WidthAuto_OnlyClearsWidth()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("w-auto"));

        var setter = Assert.Single(util.Body);
        Assert.Equal(Layoutable.WidthProperty, setter.Property);
        Assert.True(double.IsNaN(Assert.IsType<double>(setter.Value)));
    }

    [AvaloniaFact]
    public void HeightAuto_OnlyClearsHeight()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("h-auto"));

        var setter = Assert.Single(util.Body);
        Assert.Equal(Layoutable.HeightProperty, setter.Property);
        Assert.True(double.IsNaN(Assert.IsType<double>(setter.Value)));
    }

    /// <summary>
    /// A concrete size ranks below <c>full</c>, so both source orders fill: source order never decides the
    /// cascade (see ARCHITECTURE.md, "Emission order").
    /// </summary>
    [AvaloniaTheory]
    [InlineData("w-4 w-full")]
    [InlineData("w-full w-4")]
    public void WidthFull_OutranksAConcreteWidth_WhateverTheSourceOrder(string source)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "w-4", "w-full" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.True(double.IsNaN(border.Width));
            Assert.Equal(HorizontalAlignment.Stretch, border.HorizontalAlignment);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// <c>auto</c> ranks below a concrete size (Tailwind emits <c>w-auto</c> first), so the scale value survives.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("w-auto w-4")]
    [InlineData("w-4 w-auto")]
    public void WidthAuto_LosesToAConcreteWidth_WhateverTheSourceOrder(string source)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "w-auto", "w-4" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(64, border.Width);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary><c>full</c> outranks <c>auto</c> as well, so the stretch survives a <c>w-auto</c> in the same class list.</summary>
    [AvaloniaTheory]
    [InlineData("w-auto w-full")]
    [InlineData("w-full w-auto")]
    public void WidthFull_OutranksWidthAuto_WhateverTheSourceOrder(string source)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "w-auto", "w-full" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.True(double.IsNaN(border.Width));
            Assert.Equal(HorizontalAlignment.Stretch, border.HorizontalAlignment);
        }
        finally
        {
            window.Close();
        }
    }
}
