using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

public class AddMiniThemeTests
{
    [AvaloniaFact]
    public void CallingTwice_DoesNotDuplicateRulesOrVariants()
    {
        var builder = ThemeBuilder<MiniTheme>.Create();
        var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };

        builder.AddMiniTheme(configuration);
        var rulesAfterFirst = configuration.Rules.Count;
        var variantsAfterFirst = configuration.Variants.Count;

        builder.AddMiniTheme(configuration);

        Assert.Equal(rulesAfterFirst, configuration.Rules.Count);
        Assert.Equal(variantsAfterFirst, configuration.Variants.Count);
    }

    [AvaloniaFact]
    public void PopulatesEveryThemeScale()
    {
        var builder = ThemeBuilder<MiniTheme>.Create();
        var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };

        builder.AddMiniTheme(configuration);

        Assert.NotEmpty(configuration.Theme.Spacing);
        Assert.NotEmpty(configuration.Theme.Sizes);
        Assert.NotEmpty(configuration.Theme.Radii);
        Assert.NotEmpty(configuration.Theme.Colors);
        Assert.NotEmpty(configuration.Theme.FontSizes);
        Assert.NotEmpty(configuration.Theme.LineWidths);
        Assert.NotEmpty(configuration.Theme.Breakpoints);
        Assert.True(configuration.Theme.RemToPxFactor > 0);
    }
}
