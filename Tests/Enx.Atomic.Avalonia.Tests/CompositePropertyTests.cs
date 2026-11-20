using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Enx.Atomic.Avalonia.Composites;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// The host contract and the build-time half of the composite-side mechanism: the descriptors must describe
/// the real hosts field for field, and the source the emitter generates from them must compile on its own and
/// apply the composed values to real controls.
/// </summary>
public class CompositePropertyTests
{
    /// <summary>
    /// The descriptor is what the emitter trusts, so it has to describe the runtime host exactly — same type
    /// name, same field names, same registered names, same real property. A mismatch here is the drift the
    /// emitted copy would otherwise inherit silently.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(CompositeGroup.Margin)]
    [InlineData(CompositeGroup.Padding)]
    [InlineData(CompositeGroup.BorderThickness)]
    [InlineData(CompositeGroup.CornerRadius)]
    public void Descriptor_MatchesTheRuntimeHost(CompositeGroup group)
    {
        var descriptor = RuntimeCompositeHosts.Descriptor(group);
        var host = RuntimeCompositeHosts.HostType(group);
        var sides = RuntimeCompositeHosts.Sides(group).ToArray();

        Assert.Equal(host.Name, descriptor.HostTypeName);
        Assert.Equal(ExpectedKind(group), descriptor.Kind);
        Assert.Same(ExpectedRealProperty(group), descriptor.RealProperty);
        Assert.Equal(4, descriptor.SideFields.Count);
        Assert.Equal(4, descriptor.AttachedPropertyNames.Count);

        for (var index = 0; index < 4; index++)
        {
            var field = host.GetField(descriptor.SideFields[index], BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            Assert.Same(sides[index], field.GetValue(null));
            Assert.Equal(descriptor.AttachedPropertyNames[index], sides[index].Name);
            // The code generator discovers a host through its property's OwnerType, which for an attached
            // property is the declaring type passed to RegisterAttached — not the type it can be attached to.
            Assert.Equal(host, sides[index].OwnerType);
        }
    }

    /// <summary>
    /// <see cref="CompositePropertyDescriptor.Create"/> derives both name lists from one prefix, which is what
    /// makes a host impossible to half-rename: the runtime fields and the embedded copy's fields and registered
    /// names all come from the same two strings.
    /// </summary>
    [AvaloniaFact]
    public void Create_DerivesFieldAndRegisteredNamesFromOnePrefix()
    {
        var descriptor = CompositePropertyDescriptor.Create(
            Layoutable.MarginProperty,
            CompositeKind.Thickness,
            "ExampleSides",
            "Example"
        );

        Assert.Equal(
            ["ExampleLeftProperty", "ExampleTopProperty", "ExampleRightProperty", "ExampleBottomProperty"],
            descriptor.SideFields
        );
        Assert.Equal(
            ["EnxExampleLeft", "EnxExampleTop", "EnxExampleRight", "EnxExampleBottom"],
            descriptor.AttachedPropertyNames
        );

        // CornerRadius has its own slot order — its constructor is (topLeft, topRight, bottomRight, bottomLeft),
        // not the flat (left, top, right, bottom) the Thickness-valued composites use.
        var corners = CompositePropertyDescriptor.Create(
            Border.CornerRadiusProperty,
            CompositeKind.CornerRadius,
            "ExampleCorners",
            "Example"
        );

        Assert.Equal(
            [
                "ExampleTopLeftProperty",
                "ExampleTopRightProperty",
                "ExampleBottomRightProperty",
                "ExampleBottomLeftProperty",
            ],
            corners.SideFields
        );
        Assert.Equal(
            ["EnxExampleTopLeft", "EnxExampleTopRight", "EnxExampleBottomRight", "EnxExampleBottomLeft"],
            corners.AttachedPropertyNames
        );
    }

    /// <summary>
    /// The embedded copy needs no reference beyond Avalonia: one helper, one host per composite, and nothing
    /// from the core engine (which the consuming project may not even reference).
    /// </summary>
    [AvaloniaFact]
    public void EmittedSource_EmbedsEachHostOnce_AndReferencesOnlyAvalonia()
    {
        var source = EmbeddedCompositeHosts.Instance.Source;

        Assert.Equal(1, CountOccurrences(source, "file static class CompositeSideRecomposition"));
        foreach (var group in Enum.GetValues<CompositeGroup>())
            Assert.Equal(
                1,
                CountOccurrences(source, $"file static class {RuntimeCompositeHosts.HostType(group).Name}")
            );

        // Generated code must compile standalone: no core-engine reference, no dependency on the original
        // (non-generated) host's EmbeddableSource string. (The file header does name the generator, so the
        // assertion targets the host namespaces rather than the product name.)
        Assert.DoesNotContain("Enx.Atomic.Avalonia.Composites", source);
        Assert.DoesNotContain("EmbeddableSource", source);
        Assert.Contains("#nullable enable", source);
    }

    /// <summary>
    /// The end-to-end codegen check: the emitted <c>Styles</c> class, compiled by Roslyn and applied to a live
    /// headless control, produces the composed values. This is also where the boxed-value type matters —
    /// <see cref="Avalonia.Styling.Setter.Value"/> is <c>object?</c>, and an emitted <c>int</c>/<c>float</c> where
    /// the side property wants a <c>double</c> would only fail once Avalonia applies it.
    /// </summary>
    [AvaloniaFact]
    public void EmittedStyles_ApplyComposedValuesToRealControls()
    {
        var probe = EmbeddedCompositeHosts.Instance;
        var styles = probe.CreateStyles();

        var setters = CompositeTestSource.EnumerateSetters(styles).ToArray();
        Assert.Equal(16, setters.Length);
        Assert.All(setters, setter => Assert.IsType<double>(setter.Value));

        var border = new Border
        {
            Classes = { "probe-Margin", "probe-Padding", "probe-BorderThickness", "probe-CornerRadius" },
        };
        var window = new Window { Content = border, Styles = { styles } };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(1, 2, 3, 4), border.Margin);
            Assert.Equal(new Thickness(5, 6, 7, 8), border.Padding);
            Assert.Equal(new Thickness(9, 10, 11, 12), border.BorderThickness);
            Assert.Equal(new CornerRadius(13, 14, 15, 16), border.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    private static CompositeKind ExpectedKind(CompositeGroup group) =>
        group == CompositeGroup.CornerRadius ? CompositeKind.CornerRadius : CompositeKind.Thickness;

    private static AvaloniaProperty ExpectedRealProperty(CompositeGroup group) =>
        group switch
        {
            CompositeGroup.Margin => Layoutable.MarginProperty,
            CompositeGroup.Padding => Decorator.PaddingProperty,
            CompositeGroup.BorderThickness => Border.BorderThicknessProperty,
            CompositeGroup.CornerRadius => Border.CornerRadiusProperty,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null),
        };

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var index = haystack.IndexOf(needle, StringComparison.Ordinal); index >= 0; )
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
