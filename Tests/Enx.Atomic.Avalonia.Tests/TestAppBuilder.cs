using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Enx.Atomic.Avalonia.Tests.TestAppBuilder))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerAssembly)]

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// Wires the test assembly to Avalonia's headless platform, per
/// https://docs.avaloniaui.net/docs/testing/headless-xunit. No <c>App</c> subclass/XAML is needed — none of
/// these tests render controls, they just need <c>AvaloniaLocator</c> to have a platform registered (some
/// static rules construct real Avalonia types like <c>Cursor</c> at static-init time, which need one).
/// <see cref="AvaloniaTestIsolationLevel.PerAssembly"/> reuses a single app instance across all tests, which
/// is fine here since nothing mutates shared Avalonia app state.
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
