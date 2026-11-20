using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

public class SafelistTests
{
    [AvaloniaFact]
    public void Generate_WithoutSource_SplitsAndTrimsSafelistEntries()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Safelist = ["", " \t ", " w-4\t h-8\nopacity-50 ", "w-4"];

        var styles = generator.Generate(string.Empty, new());

        Assert.Equal(3, styles.Length);
        Assert.Contains(styles, style => style.ResolveSelector().ToString().Contains(".w-4"));
        Assert.Contains(styles, style => style.ResolveSelector().ToString().Contains(".h-8"));
        Assert.Contains(styles, style => style.ResolveSelector().ToString().Contains(".opacity-50"));
        Assert.Empty(generator.Diagnostics);
    }

    [AvaloniaFact]
    public void Generate_DeduplicatesWithoutMutatingCallerTokens()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Safelist = ["w-4 hover:w-8"];
        ISet<string> tokens = new HashSet<string> { "w-4" };

        var styles = generator.Generate(tokens, new());

        Assert.Collection(
            styles,
            style => Assert.Contains(".w-4", style.ResolveSelector().ToString()),
            style => Assert.Contains(".hover:w-8", style.ResolveSelector().ToString())
        );
        Assert.Equal("w-4", Assert.Single(tokens));
    }

    [AvaloniaFact]
    public void Generate_WithSafelistDisabled_IgnoresConfigurationAndPerCallEntries()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Safelist = ["w-8 unknown-config-token"];

        var styles = generator.Generate(
            "w-4",
            new() { IncludeSafelist = false, Safelist = ["h-4 unknown-per-call-token"] }
        );

        Assert.Contains(".w-4", Assert.Single(styles).ResolveSelector().ToString());
        Assert.Empty(generator.Diagnostics);
    }

    [AvaloniaFact]
    public void Generate_WarnsOncePerUnmatchedSafelistedToken_EvenIfAlsoExtracted()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        const string input = "only-extracted-miss shared-miss";

        // A previous ordinary extraction can already have cached the miss.
        Assert.Empty(generator.Generate(input, new()));
        Assert.Empty(generator.Diagnostics);

        configuration.Safelist = ["shared-miss only-safelisted-miss", "shared-miss"];
        var options = new AtomicGenerator<MiniTheme>.Options { Safelist = [" only-safelisted-miss "] };
        Assert.Empty(generator.Generate(input, options));
        Assert.Empty(generator.Generate(input, options));

        Assert.Equal(
            [
                "Safelisted token 'shared-miss' matched no rule",
                "Safelisted token 'only-safelisted-miss' matched no rule",
            ],
            generator.Diagnostics
        );
    }
}
