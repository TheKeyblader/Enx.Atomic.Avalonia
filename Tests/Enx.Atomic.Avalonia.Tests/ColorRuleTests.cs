using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Enx.Atomic.Avalonia.Tests;

public class ColorRuleTests
{
    [AvaloniaFact]
    public void BackgroundColor_TargetsBorderTemplatedControlAndPanel()
    {
        // Regression test: Border.BackgroundProperty and TemplatedControl.BackgroundProperty are the exact
        // same AvaloniaProperty instance (TemplatedControl adds itself as an owner of Border's property), so
        // its OwnerType always reports Border regardless of which static field it was accessed through.
        // BackgroundColorRule relies on an explicit StyleValue.TargetType override to still produce separately
        // selected styles for TemplatedControl (and therefore anything deriving from it, like Button) and for
        // Panel (StackPanel, Grid, DockPanel, ...) — without those overrides, "bg-red-500" would never
        // visibly apply to those elements.
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("bg-red-500");

        Assert.Equal(3, results.Length);

        var borderUtil = Assert.Single(results, u => u.ResolveSelector().ToString().Contains(nameof(Border)));
        var templatedControlUtil = Assert.Single(
            results,
            u => u.ResolveSelector().ToString().Contains(nameof(TemplatedControl))
        );
        var panelUtil = Assert.Single(results, u => u.ResolveSelector().ToString().Contains(nameof(Panel)));

        foreach (var util in new[] { borderUtil, templatedControlUtil, panelUtil })
        {
            var setter = Assert.Single(util.Body);
            Assert.Equal(Border.BackgroundProperty, setter.Property);
            // bg-* now resolves to a resource-based value (StyleValue.Resource), not a fixed literal — see
            // BackgroundColorRule and StyleValue.Resource — so the raw brush was never inlined at all.
            var resource = Assert.IsType<DynamicResourceExtension>(setter.Value);
            Assert.Equal("Colors[red-500]", resource.ResourceKey);
        }
    }

    [AvaloniaFact]
    public void BackgroundColor_ResourceResolvesToTheThemeBrush()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("bg-red-500");

        Assert.Equal(3, results.Length);
        var resourceKey = Assert.Single(generator.ResolvedResources.Keys);
        var entry = generator.ResolvedResources[resourceKey];
        var resolved = entry.ThemeAccess.Compile().DynamicInvoke(configuration.Theme);
        var themed = Assert.IsAssignableFrom<IThemedValue>(resolved);
        Assert.False(themed.IsThemed);
        Assert.Same(configuration.Theme.Colors["red-500"].Light, themed.LightValue);
    }
}
