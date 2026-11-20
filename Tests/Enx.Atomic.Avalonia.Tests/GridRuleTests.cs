using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Enx.Atomic.Avalonia.Preset.Mini;
using Enx.Atomic.Avalonia.Preset.Mini.Dynamic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Enx.Atomic.Avalonia.Tests;

public class GridRuleTests
{
    [AvaloniaFact]
    public void EmbeddableSource_CompilesAndAppliesToARealGrid()
    {
        // The real (non-embedded) GridDefinitions now registers the same class handlers in its own static
        // constructor (see GridCols_AtRuntime_Applies...), for the runtime path. EmbeddableSource is still what a
        // consuming project's generated code compiles and runs — the build-time path must stay standalone — so
        // this compiles that text for real (the same way CodeGenTests compiles emitted styles) and proves it
        // still applies to a real Grid.
        var source = $$"""
            namespace GridDefinitionsEmbedProbe;

            {{GridDefinitions.EmbeddableSource}}

            public static class Probe
            {
                public static global::Avalonia.AvaloniaProperty ColumnProperty() => GridDefinitions.ColumnDefinitionsProperty;

                public static global::Avalonia.AvaloniaProperty RowProperty() => GridDefinitions.RowDefinitionsProperty;
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "GridDefinitionsEmbedProbeAssembly",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(
            result.Success,
            string.Join('\n', result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)) + "\n\n" + source
        );

        stream.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(stream.ToArray());
        var probeType = assembly.GetType("GridDefinitionsEmbedProbe.Probe")!;
        var columnProperty = (AvaloniaProperty)probeType.GetMethod("ColumnProperty")!.Invoke(null, null)!;
        var rowProperty = (AvaloniaProperty)probeType.GetMethod("RowProperty")!.Invoke(null, null)!;

        var grid = new Grid();
        grid.SetValue(columnProperty, new ColumnDefinitions("1*,1*,1*"));
        grid.SetValue(rowProperty, new RowDefinitions("1*,1*"));

        Assert.Equal(3, grid.ColumnDefinitions.Count);
        Assert.All(grid.ColumnDefinitions, c => Assert.Equal(GridLength.Star, c.Width));
        Assert.Equal(2, grid.RowDefinitions.Count);
        Assert.All(grid.RowDefinitions, r => Assert.Equal(GridLength.Star, r.Height));
    }

    [AvaloniaFact]
    public void GridCols_ResolvesToColumnDefinitionsGhostPropertyTargetingGrid()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("grid-cols-4");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(GridDefinitions.ColumnDefinitionsProperty, setter.Property);
        var columns = Assert.IsType<ColumnDefinitions>(setter.Value);
        Assert.Equal(4, columns.Count);
        Assert.Contains(nameof(Grid), util.ResolveSelector().ToString());
    }

    [AvaloniaFact]
    public void GridRows_ResolvesToRowDefinitionsGhostPropertyTargetingGrid()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken("grid-rows-2");

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(GridDefinitions.RowDefinitionsProperty, setter.Property);
        var rows = Assert.IsType<RowDefinitions>(setter.Value);
        Assert.Equal(2, rows.Count);
    }

    /// <summary>
    /// The runtime path: <see cref="AtomicGenerator{TTheme}.Generate"/> resolves the ghost property, and
    /// <see cref="GridDefinitions"/>'s own static constructor registers the class handler that syncs it into
    /// <see cref="Grid.ColumnDefinitions"/>/<see cref="Grid.RowDefinitions"/>. Before that static constructor
    /// existed only the embedded copy did the sync, so the resolved style silently did nothing.
    /// </summary>
    [AvaloniaFact]
    public void GridColsAndRows_AtRuntime_ApplyToAShownGrid()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("grid-cols-3 grid-rows-2", new AtomicGenerator<MiniTheme>.Options());
        var grid = new Grid { Classes = { "grid-cols-3", "grid-rows-2" } };

        var window = utils.ShowWithStyles(grid);
        try
        {
            Assert.Equal(3, grid.ColumnDefinitions.Count);
            Assert.All(grid.ColumnDefinitions, column => Assert.Equal(GridLength.Star, column.Width));
            Assert.Equal(2, grid.RowDefinitions.Count);
            Assert.All(grid.RowDefinitions, row => Assert.Equal(GridLength.Star, row.Height));
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary><c>col-span-*</c>/<c>row-*</c> are ordinary attached properties on the grid *child* and work at runtime already — this pins that the grid test above didn't change them.</summary>
    [AvaloniaFact]
    public void GridCell_AtRuntime_AppliesToAGridChild()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate("grid-cols-3 col-span-2 row-1", new AtomicGenerator<MiniTheme>.Options());
        var grid = new Grid { Classes = { "grid-cols-3" } };
        var child = new Border { Classes = { "col-span-2", "row-1" } };
        grid.Children.Add(child);

        var window = utils.ShowWithStyles(grid);
        try
        {
            Assert.Equal(3, grid.ColumnDefinitions.Count);
            Assert.Equal(2, Grid.GetColumnSpan(child));
            Assert.Equal(1, Grid.GetRow(child));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("col-2", 2)]
    [InlineData("row-1", 1)]
    [InlineData("col-span-3", 3)]
    [InlineData("row-span-4", 4)]
    public void GridCell_SetsTheRealAttachedProperty(string token, int expected)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken(token);

        var util = Assert.Single(results);
        var setter = Assert.Single(util.Body);
        Assert.Equal(expected, setter.Value);

        // Regression test: these are attached properties settable on any Control (a grid *child*, not the
        // Grid itself) — StyleValue.TargetType must default to StyledElement for an IsAttached property
        // instead of the property's OwnerType (Grid), or col-span-3 etc. would never match anything but Grid
        // elements themselves. See StyleValue.DefaultTargetType.
        Assert.Contains(nameof(StyledElement), util.ResolveSelector().ToString());
        Assert.DoesNotContain(nameof(Grid), util.ResolveSelector().ToString());
    }

    [AvaloniaTheory]
    [InlineData("col-span-0")]
    [InlineData("row-span-0")]
    [InlineData("col--1")]
    public void GridCell_InvalidValue_DoesNotMatch(string token)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var results = generator.ParseToken(token);

        Assert.Empty(results);
    }
}
