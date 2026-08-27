using Avalonia.Styling;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>Builds a fresh, fully-wired Mini preset generator for a test. Each call is independent — no shared/cached state between tests.</summary>
internal static class TestHelpers
{
    public static (
        AtomicConfiguration<MiniTheme> Configuration,
        AtomicGenerator<MiniTheme> Generator
    ) CreateMiniGenerator()
    {
        var builder = ThemeBuilder<MiniTheme>.Create();
        var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };
        builder.AddMiniTheme(configuration);
        var generator = new AtomicGenerator<MiniTheme>(configuration);
        return (configuration, generator);
    }

    public static Selector ResolveSelector(this StringifiedUtil util) => util.Selector.Compile()(null!);

    public static StyleQuery? ResolveContainerQuery(this StringifiedUtil util) => util.ContainerQuery?.Compile()(null!);
}
