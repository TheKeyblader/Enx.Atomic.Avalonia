using Avalonia;
using Avalonia.Headless;
using Enx.Atomic.Avalonia;
using Enx.Atomic.Avalonia.CodeGen;
using Enx.Atomic.Avalonia.Preset.Mini;

// A headless platform is enough — this never shows a window, it just needs AvaloniaLocator populated (some
// static rules, e.g. Cursors, construct real Avalonia types at static-init time).
AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();

var builder = ThemeBuilder<MiniTheme>.Create();
var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };
builder.AddMiniTheme(configuration);

return AtomicCli.Run(args, configuration);
