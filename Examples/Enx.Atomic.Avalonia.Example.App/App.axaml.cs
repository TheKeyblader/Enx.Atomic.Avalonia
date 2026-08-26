using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Enx.Atomic.Avalonia.Example.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
        // GenResources.g.cs (AtomicResources), like GenStyles.g.cs/AtomicStyles below, doesn't exist yet when
        // App.axaml is parsed by the XAML compiler — merged here instead of via <ResourceDictionary.MergedDictionaries>.
        Resources.MergedDictionaries.Add(AtomicResources.Build());
        Styles.Insert(0, new AtomicStyles());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
