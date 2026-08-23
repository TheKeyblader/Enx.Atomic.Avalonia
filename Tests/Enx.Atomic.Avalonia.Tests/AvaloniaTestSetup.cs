using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// Some static rules (e.g. <c>Cursors</c>) construct real Avalonia types (<c>Cursor</c>) at static-init time,
/// which need a platform registered in <c>AvaloniaLocator</c> — otherwise resolving any of them throws. Sets
/// up Avalonia's headless platform once, before any test runs, so no test needs a real display.
/// </summary>
internal static class AvaloniaTestSetup
{
    [ModuleInitializer]
    public static void Initialize() =>
        AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
}
