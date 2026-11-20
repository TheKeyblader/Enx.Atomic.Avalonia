using Avalonia.Controls;
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

    /// <summary>
    /// Applies resolved <paramref name="utils"/> to a freshly shown headless <see cref="Window"/> holding
    /// <paramref name="controls"/> in a <see cref="StackPanel"/>, so a test can assert the property values
    /// Avalonia actually ends up applying rather than only the generated setters. Styles are added in the order
    /// produced, matching <c>StyleEmitter</c>'s output (later styles win, as in a real <c>Styles</c> collection).
    /// A container-query-scoped util is wrapped in a top-level <see cref="ContainerQuery"/>, as <c>StyleEmitter</c>
    /// does (a <see cref="ContainerQuery"/> can't be nested inside a <see cref="Style"/>, but is fine at the root of
    /// a <c>Styles</c> collection).
    /// <para>
    /// The <see cref="StackPanel"/> is registered as a container named <c>top-level</c> (<c>AtomicCli</c>'s
    /// default) with <see cref="ContainerSizing.Width"/>, so breakpoint utilities actually activate: Avalonia only
    /// installs the query provider that tracks a container's size when <see cref="Container.SizingProperty"/> is
    /// set to a real mode, not <see cref="ContainerSizing.Normal"/>. The window is sized to the <c>width</c>
    /// argument (<c>NaN</c> means "let the platform pick"), which is what the container's available width — the
    /// size a width query compares against — ends up being; a test can resize it afterwards with
    /// <c>window.Width = ...</c> followed by <c>Dispatcher.UIThread.RunJobs()</c>.
    /// </para>
    /// The caller owns the returned window and should close it when done.
    /// </summary>
    public static Window ShowWithStyles(this IEnumerable<StringifiedUtil> utils, params Control[] controls) =>
        ShowWithStyles(utils, 800, controls);

    /// <param name="width">The window's initial width, i.e. the container's available width — see the summary above.</param>
    /// <param name="controls">The controls to show, in a container <see cref="StackPanel"/>.</param>
    public static Window ShowWithStyles(
        this IEnumerable<StringifiedUtil> utils,
        double width,
        params Control[] controls
    )
    {
        const string containerName = "top-level";
        var materialized = utils.ToArray();
        var panel = new StackPanel();
        Container.SetName(panel, containerName);
        Container.SetSizing(panel, ContainerSizing.Width);
        foreach (var control in controls)
            panel.Children.Add(control);

        var window = new Window
        {
            Content = panel,
            Width = width,
            Height = 400,
        };

        foreach (var util in materialized)
        {
            var compiled = util.Selector.Compile();
            var style = new Style(selector => compiled(selector!));
            foreach (var setter in util.Body)
                style.Setters.Add(setter);

            if (util.ContainerQuery is null)
            {
                window.Styles.Add(style);
                continue;
            }

            var compiledQuery = util.ContainerQuery.Compile();
            var query = new ContainerQuery(q => compiledQuery(q!), containerName);
            query.Children.Add(style);
            window.Styles.Add(query);
        }

        window.Show();
        return window;
    }

    /// <summary>Adds/removes an Avalonia pseudo-class on <paramref name="control"/>, e.g. <c>:pointerover</c>.</summary>
    public static void SetPseudoClass(this Control control, string pseudoClass, bool value)
    {
        var classes = (IPseudoClasses)control.Classes;
        if (value)
            classes.Add(pseudoClass);
        else
            classes.Remove(pseudoClass);
    }
}
