using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

public class GhostPropertyCombinerTests
{
    [AvaloniaFact]
    public void CoOccurringSides_CombineIntoOneRealMarginValue_WithACompoundSelector()
    {
        // AddMiniTheme registers GhostPropertyCombiner<MiniTheme> automatically.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.Generate(
            "Classes=\"ml-1 mr-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Layoutable.MarginProperty, setter.Property);
        Assert.Equal(new Thickness(4, 0, 8, 0), setter.Value);

        var selector = util.ResolveSelector().ToString();
        Assert.Contains(".ml-1", selector);
        Assert.Contains(".mr-2", selector);
    }

    [AvaloniaFact]
    public void LoneGhostToken_StillYieldsItsOwnStyle()
    {
        // A "group" of one: no sibling to combine with, but it should still resolve — otherwise a ghost
        // property could never be used on its own.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.Generate(
            "Classes=\"mt-4\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Layoutable.MarginProperty, setter.Property);
        Assert.Equal(new Thickness(0, 16, 0, 0), setter.Value);
        Assert.Contains(".mt-4", util.ResolveSelector().ToString());
    }

    [AvaloniaFact]
    public void OriginalGhostTokens_NeverReachFinalOutputOnTheirOwn()
    {
        // ml-1/mr-2 resolve to a SpecialProperties-scoped style on their own, dropped at Generate()'s
        // emission boundary — the transformer's compound-selector style is the only one that reaches output.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.Generate(
            "Classes=\"ml-1 mr-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        Assert.Single(results);
    }

    [AvaloniaFact]
    public void SourceTextIsNeverRewritten()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var transformed = generator.ApplyTransformers("Classes=\"ml-1 mr-2\"", "test.axaml");

        Assert.Equal("Classes=\"ml-1 mr-2\"", transformed);
    }

    [AvaloniaFact]
    public void UnrelatedToken_OnTheSameLine_IsUnaffected()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.Generate(
            "Classes=\"p-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Decorator.PaddingProperty, setter.Property);
        Assert.Equal(new Thickness(8), setter.Value);
    }

    [AvaloniaFact]
    public void PaddingSides_CombineIntoOneRealPaddingValue()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.Generate(
            "Classes=\"pl-2 pt-4\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Decorator.PaddingProperty, setter.Property);
        Assert.Equal(new Thickness(8, 16, 0, 0), setter.Value);
    }

    [AvaloniaFact]
    public void CornerRadii_CombineIntoOneRealCornerRadiusValue()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.Generate(
            "Classes=\"rounded-tl-lg rounded-tr-md\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Border.CornerRadiusProperty, setter.Property);
        Assert.Equal(new CornerRadius(8, 6, 0, 0), setter.Value);
    }

    [AvaloniaFact]
    public void SameCombination_OnDifferentLines_YieldsOnlyOneStyle()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.Generate(
            "Classes=\"ml-1 mr-2\"\nClasses=\"ml-1 mr-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        Assert.Single(results);
    }
}
