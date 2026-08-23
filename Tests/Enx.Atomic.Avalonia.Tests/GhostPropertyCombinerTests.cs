using Enx.Atomic.Avalonia.Preset.Mini;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Enx.Atomic.Avalonia.Tests;

public class GhostPropertyCombinerTests
{
    [AvaloniaFact]
    public void CoOccurringSides_CombineIntoOneRealMarginValue()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(new GhostPropertyCombiner<MiniTheme>());

        var results = generator.Generate(
            "Classes=\"ml-1 mr-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Layoutable.MarginProperty, setter.Property);
        Assert.Equal(new Thickness(4, 0, 8, 0), setter.Value);
    }

    [AvaloniaFact]
    public void LoneGhostToken_StillFallsBackToItsOwnStyle()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(new GhostPropertyCombiner<MiniTheme>());

        var results = generator.Generate(
            "Classes=\"mt-4\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(Layoutable.MarginProperty, setter.Property);
        Assert.Equal(new Thickness(0, 16, 0, 0), setter.Value);
    }

    [AvaloniaFact]
    public void OriginalGhostTokens_NeverReachFinalOutputOnTheirOwn()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(new GhostPropertyCombiner<MiniTheme>());

        var results = generator.Generate(
            "Classes=\"ml-1 mr-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        // Only the synthesized combined style should show up - not the raw ml-1/mr-2 tokens resolving to
        // their (unemittable) SpecialProperties-scoped styles.
        Assert.Single(results);
    }

    [AvaloniaFact]
    public void UnrelatedToken_OnTheSameLine_IsUnaffected()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(new GhostPropertyCombiner<MiniTheme>());

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
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(new GhostPropertyCombiner<MiniTheme>());

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
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(new GhostPropertyCombiner<MiniTheme>());

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
    public void SameCombination_OnDifferentLines_RegistersOnlyOneSyntheticRule()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(new GhostPropertyCombiner<MiniTheme>());
        var rulesBefore = configuration.Rules.Count;

        generator.Generate(
            "Classes=\"ml-1 mr-2\"\nClasses=\"ml-1 mr-2\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
        );

        // Two lines with the same combination should register exactly one synthetic Rule.Static, not two.
        Assert.Equal(rulesBefore + 1, configuration.Rules.Count);
    }
}
