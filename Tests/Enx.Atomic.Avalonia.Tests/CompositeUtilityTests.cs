using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Enx.Atomic.Avalonia.Composites;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// The padding, border-width and corner-radius families after they moved onto the composite side hosts
/// (<see cref="PaddingSides"/>, <see cref="BorderThicknessSides"/>, <see cref="CornerRadiusSides"/>).
///
/// <para>
/// Where a scenario can be expressed as a real property value, it is asserted on a shown headless control rather
/// than on the generated setters — that is the half the old ghost-property combiner got wrong (a variant lost its
/// pseudo-class/container query, and overlapping utilities overwrote each other's whole struct). The setter-shape
/// tests are kept only for the parts a value assertion cannot distinguish, such as which side a token targets.
/// </para>
/// </summary>
public class CompositeUtilityTests
{
    [AvaloniaFact]
    public void Padding_UniformVariant_WritesAllFourSides()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("p-4"));

        Assert.Equal(4, util.Body.Length);
        Assert.All(util.Body, setter => Assert.Equal(16d, Assert.IsType<double>(setter.Value)));
        Assert.Equal(
            [
                PaddingSides.PaddingLeftProperty,
                PaddingSides.PaddingTopProperty,
                PaddingSides.PaddingRightProperty,
                PaddingSides.PaddingBottomProperty,
            ],
            util.Body.Select(setter => setter.Property)
        );
    }

    [AvaloniaFact]
    public void PaddingSide_TargetsItsOwnSideProperty()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("pt-4"));

        var setter = Assert.Single(util.Body);
        Assert.Equal(PaddingSides.PaddingTopProperty, setter.Property);
        Assert.Equal(16d, Assert.IsType<double>(setter.Value));
    }

    [AvaloniaFact]
    public void PaddingAxis_TargetsBothSidesForThatAxis()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("px-2"));

        var properties = util.Body.Select(setter => setter.Property).ToArray();
        Assert.Equal(2, properties.Length);
        Assert.Contains(PaddingSides.PaddingLeftProperty, properties);
        Assert.Contains(PaddingSides.PaddingRightProperty, properties);
        Assert.All(util.Body, setter => Assert.Equal(8d, Assert.IsType<double>(setter.Value)));
    }

    /// <summary>
    /// A lone side token still resolves on its own — the case the old combiner handled as a "group of one", kept
    /// here as an actual property value rather than a compound-selector style.
    /// </summary>
    [AvaloniaFact]
    public void PaddingSide_AppliesOnItsOwn()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"pt-4\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var border = new Border { Classes = { "pt-4" } };
        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(new Thickness(0, 16, 0, 0), border.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Bug 6's guard extended to every control that registers the <c>AddOwner</c>'ed Padding property: the token
    /// targets <c>Control</c>, so a <see cref="Button"/> (a <see cref="TemplatedControl"/>), a
    /// <see cref="Border"/> (<see cref="Decorator"/>) and a <see cref="TextBlock"/> all receive it.
    /// </summary>
    [AvaloniaFact]
    public void Padding_AppliesToButtonBorderAndTextBlock()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("Classes=\"p-4\"", new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" });

        var button = new Button { Classes = { "p-4" } };
        var border = new Border { Classes = { "p-4" } };
        var textBlock = new TextBlock { Classes = { "p-4" } };
        var window = utils.ShowWithStyles(button, border, textBlock);
        try
        {
            Assert.Equal(new Thickness(16), button.Padding);
            Assert.Equal(new Thickness(16), border.Padding);
            Assert.Equal(new Thickness(16), textBlock.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("p-4 px-2")]
    [InlineData("px-2 p-4")]
    public void PaddingAxis_OverridesUniform_WhateverTheSourceOrder(string classes)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            $"Classes=\"{classes}\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var border = WithClasses(new Border(), classes);
        var window = utils.ShowWithStyles(border);
        try
        {
            // UnoCSS's rule order: the axis entry outranks the uniform one, so left/right are 8 and top/bottom 16.
            Assert.Equal(new Thickness(8, 16, 8, 16), border.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PaddingHoverVariant_AppliesOnlyWhilePointerOver()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"hover:p-4\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var button = new Button { Classes = { "hover:p-4" } };
        var window = utils.ShowWithStyles(button);
        try
        {
            var resting = button.Padding;
            Assert.NotEqual(new Thickness(16), resting);

            button.SetPseudoClass(":pointerover", true);
            Assert.Equal(new Thickness(16), button.Padding);

            button.SetPseudoClass(":pointerover", false);
            Assert.Equal(resting, button.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BorderWidth_UniformVariant_WritesAllFourSides()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("border-2"));

        Assert.Equal(4, util.Body.Length);
        Assert.All(util.Body, setter => Assert.Equal(2d, Assert.IsType<double>(setter.Value)));
        Assert.Equal(
            [
                BorderThicknessSides.BorderThicknessLeftProperty,
                BorderThicknessSides.BorderThicknessTopProperty,
                BorderThicknessSides.BorderThicknessRightProperty,
                BorderThicknessSides.BorderThicknessBottomProperty,
            ],
            util.Body.Select(setter => setter.Property)
        );
    }

    /// <summary>
    /// <see cref="Border.BorderThicknessProperty"/> is registered by <see cref="Border"/> and <c>AddOwner</c>'ed by
    /// <see cref="TemplatedControl"/>; targeting <c>Control</c> is what makes the same token reach both.
    /// </summary>
    [AvaloniaFact]
    public void BorderWidth_AppliesToButtonAndBorder()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"border-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var button = new Button { Classes = { "border-2" } };
        var border = new Border { Classes = { "border-2" } };
        var window = utils.ShowWithStyles(button, border);
        try
        {
            Assert.Equal(new Thickness(2), button.BorderThickness);
            Assert.Equal(new Thickness(2), border.BorderThickness);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>A lone side token composes only that side; the other three fall back to the struct's zero, not to the theme.</summary>
    [AvaloniaFact]
    public void BorderWidthSide_AppliesOnItsOwn()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"border-t-4\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var button = new Button { Classes = { "border-t-4" } };
        var border = new Border { Classes = { "border-t-4" } };
        var window = utils.ShowWithStyles(button, border);
        try
        {
            Assert.Equal(new Thickness(0, 4, 0, 0), button.BorderThickness);
            Assert.Equal(new Thickness(0, 4, 0, 0), border.BorderThickness);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("border-2 border-t-4")]
    [InlineData("border-t-4 border-2")]
    public void BorderWidthSide_OverridesUniform_WhateverTheSourceOrder(string classes)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            $"Classes=\"{classes}\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var button = WithClasses(new Button(), classes);
        var border = WithClasses(new Border(), classes);
        var window = utils.ShowWithStyles(button, border);
        try
        {
            // The uniform entry is first in UnoCSS's order: top is 4, every other side keeps the uniform 2.
            Assert.Equal(new Thickness(2, 4, 2, 2), button.BorderThickness);
            Assert.Equal(new Thickness(2, 4, 2, 2), border.BorderThickness);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BorderWidthBreakpointVariant_KeepsItsContainerQuery()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"sm:border-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var util = Assert.Single(utils);
        Assert.Equal("min-width:640", util.ResolveContainerQuery()!.ToString());

        var border = new Border { Classes = { "sm:border-2" } };
        var window = utils.ShowWithStyles(800, border);
        try
        {
            Assert.Equal(new Thickness(2), border.BorderThickness);

            window.Width = 500;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(default(Thickness), border.BorderThickness);

            window.Width = 800;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new Thickness(2), border.BorderThickness);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Rounded_UniformVariant_WritesAllFourCornersInSlotOrder()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken("rounded-md"));

        Assert.Equal(4, util.Body.Length);
        Assert.All(util.Body, setter => Assert.Equal(6d, Assert.IsType<double>(setter.Value)));
        Assert.Equal(
            [
                CornerRadiusSides.CornerRadiusTopLeftProperty,
                CornerRadiusSides.CornerRadiusTopRightProperty,
                CornerRadiusSides.CornerRadiusBottomRightProperty,
                CornerRadiusSides.CornerRadiusBottomLeftProperty,
            ],
            util.Body.Select(setter => setter.Property)
        );
    }

    public static TheoryData<string, AvaloniaProperty[]> RoundedSideCases =>
        new()
        {
            {
                "rounded-t-lg",
                [CornerRadiusSides.CornerRadiusTopLeftProperty, CornerRadiusSides.CornerRadiusTopRightProperty]
            },
            {
                "rounded-b-lg",
                [CornerRadiusSides.CornerRadiusBottomRightProperty, CornerRadiusSides.CornerRadiusBottomLeftProperty]
            },
            {
                "rounded-l-lg",
                [CornerRadiusSides.CornerRadiusTopLeftProperty, CornerRadiusSides.CornerRadiusBottomLeftProperty]
            },
            {
                "rounded-r-lg",
                [CornerRadiusSides.CornerRadiusTopRightProperty, CornerRadiusSides.CornerRadiusBottomRightProperty]
            },
            { "rounded-tl-lg", [CornerRadiusSides.CornerRadiusTopLeftProperty] },
            { "rounded-tr-lg", [CornerRadiusSides.CornerRadiusTopRightProperty] },
            { "rounded-br-lg", [CornerRadiusSides.CornerRadiusBottomRightProperty] },
            { "rounded-bl-lg", [CornerRadiusSides.CornerRadiusBottomLeftProperty] },
        };

    [AvaloniaTheory]
    [MemberData(nameof(RoundedSideCases))]
    public void RoundedSide_TargetsItsOwnCorners(string token, AvaloniaProperty[] properties)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var util = Assert.Single(generator.ParseToken(token));

        Assert.Equal(properties, util.Body.Select(setter => setter.Property));
        Assert.All(util.Body, setter => Assert.Equal(8d, Assert.IsType<double>(setter.Value)));
    }

    /// <summary>
    /// <see cref="Border.CornerRadiusProperty"/> is registered by <see cref="Border"/> and <c>AddOwner</c>'ed by
    /// <see cref="TemplatedControl"/> — and <see cref="CornerRadius"/>'s slot order is TopLeft/TopRight/
    /// BottomRight/BottomLeft, so the assertion also guards the host's slot mapping.
    /// </summary>
    [AvaloniaFact]
    public void Rounded_AppliesToButtonAndBorder()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"rounded-md\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var button = new Button { Classes = { "rounded-md" } };
        var border = new Border { Classes = { "rounded-md" } };
        var window = utils.ShowWithStyles(button, border);
        try
        {
            Assert.Equal(new CornerRadius(6), button.CornerRadius);
            Assert.Equal(new CornerRadius(6), border.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>A lone corner token composes only that corner — the slot-order check through a real value.</summary>
    [AvaloniaFact]
    public void RoundedCorner_AppliesOnItsOwn()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"rounded-tl-lg\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var button = new Button { Classes = { "rounded-tl-lg" } };
        var border = new Border { Classes = { "rounded-tl-lg" } };
        var window = utils.ShowWithStyles(button, border);
        try
        {
            Assert.Equal(new CornerRadius(8, 0, 0, 0), button.CornerRadius);
            Assert.Equal(new CornerRadius(8, 0, 0, 0), border.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("rounded-md rounded-tl-none")]
    [InlineData("rounded-tl-none rounded-md")]
    public void RoundedCorner_OverridesUniform_WhateverTheSourceOrder(string classes)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            $"Classes=\"{classes}\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var button = WithClasses(new Button(), classes);
        var border = WithClasses(new Border(), classes);
        var window = utils.ShowWithStyles(button, border);
        try
        {
            // Only the single-corner entry is emitted after the uniform one, so it overrides that one corner.
            Assert.Equal(new CornerRadius(0, 6, 6, 6), button.CornerRadius);
            Assert.Equal(new CornerRadius(0, 6, 6, 6), border.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RoundedHoverVariant_AppliesOnlyWhilePointerOver()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "Classes=\"hover:rounded-lg\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var border = new Border { Classes = { "hover:rounded-lg" } };
        var window = utils.ShowWithStyles(border);
        try
        {
            Assert.Equal(default(CornerRadius), border.CornerRadius);

            border.SetPseudoClass(":pointerover", true);
            Assert.Equal(new CornerRadius(8), border.CornerRadius);

            border.SetPseudoClass(":pointerover", false);
            Assert.Equal(default(CornerRadius), border.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SameToken_OnDifferentSourceLines_IsEmittedOnce()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var utils = generator.Generate(
            "Classes=\"p-4\"\nClasses=\"p-4\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        Assert.Single(utils);
    }

    private static T WithClasses<T>(T control, string classes)
        where T : Control
    {
        foreach (var className in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            control.Classes.Add(className);

        return control;
    }
}
