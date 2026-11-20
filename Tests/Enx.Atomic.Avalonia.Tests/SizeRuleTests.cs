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

    /// <summary>A <c>size-*</c> token writes both lengths; it resolves exactly like <c>w-*</c>/<c>h-*</c>.</summary>
    [AvaloniaFact]
    public void Size_TargetsWidthAndHeight()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("size-xs"));

        Assert.Equal(
            [Layoutable.WidthProperty, Layoutable.HeightProperty],
            util.Body.Select(setter => setter.Property)
        );
        Assert.All(util.Body, setter => Assert.Equal(320d, Assert.IsType<double>(setter.Value)));
    }

    [AvaloniaTheory]
    [InlineData("size-4", 64)]
    [InlineData("size-[123px]", 123)]
    public void Size_BareAndArbitraryValuesResolveToPixels(string token, double expected)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        Assert.All(util.Body, setter => Assert.Equal(expected, Assert.IsType<double>(setter.Value)));
    }

    [AvaloniaFact]
    public void SizeMinAndMax_TargetTheMatchingBoundProperties()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var min = Assert.Single(generator.ParseToken("size-min-4"));
        Assert.Equal(
            [Layoutable.MinWidthProperty, Layoutable.MinHeightProperty],
            min.Body.Select(setter => setter.Property)
        );

        var max = Assert.Single(generator.ParseToken("size-max-md"));
        Assert.Equal(
            [Layoutable.MaxWidthProperty, Layoutable.MaxHeightProperty],
            max.Body.Select(setter => setter.Property)
        );
        Assert.All(max.Body, setter => Assert.Equal(448d, Assert.IsType<double>(setter.Value)));
    }

    [AvaloniaFact]
    public void SizeAuto_ClearsBothLengthsOnly()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("size-auto"));

        Assert.Equal(2, util.Body.Length);
        Assert.All(util.Body, setter => Assert.True(double.IsNaN(Assert.IsType<double>(setter.Value))));
    }

    [AvaloniaFact]
    public void SizeFull_ClearsBothLengthsAndStretchesBothAxes()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("size-full", new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "size-full" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.True(double.IsNaN(border.Width));
            Assert.True(double.IsNaN(border.Height));
            Assert.Equal(HorizontalAlignment.Stretch, border.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Stretch, border.VerticalAlignment);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// <c>size-*</c> is registered before <c>w-*</c>/<c>h-*</c> (as upstream declares them), so an axis-specific
    /// token wins only its own property, in either source order: the width is <c>w-8</c>'s 128px, the height stays
    /// <c>size-4</c>'s 64px.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("size-4 w-8")]
    [InlineData("w-8 size-4")]
    public void Size_LosesItsWidthToAnAxisTokenButKeepsItsHeight(string source)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "size-4", "w-8" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(128, border.Width);
            Assert.Equal(64, border.Height);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Within the rule, <c>full</c> outranks a concrete size on both axes, whatever the source order.</summary>
    [AvaloniaTheory]
    [InlineData("size-4 size-full")]
    [InlineData("size-full size-4")]
    public void SizeFull_OutranksAConcreteSize(string source)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(source, new AtomicGenerator<MiniTheme>.Options());
        var border = new Border { Classes = { "size-4", "size-full" } };

        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.True(double.IsNaN(border.Width));
            Assert.True(double.IsNaN(border.Height));
            Assert.Equal(HorizontalAlignment.Stretch, border.HorizontalAlignment);
        }
        finally
        {
            window.Close();
        }
    }
}
