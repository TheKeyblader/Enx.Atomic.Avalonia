using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.CodeGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Enx.Atomic.Avalonia.Tests;

public class ThemedTests
{
    [Fact]
    public void ImplicitConversion_FromPlainValue_IsNotThemed()
    {
        Themed<int> themed = 5;

        Assert.False(themed.IsThemed);
        Assert.Equal(5, themed.Light);
        Assert.Equal(5, themed.Dark);
    }

    [Fact]
    public void TwoValueConstructor_IsThemed()
    {
        var themed = new Themed<int>(1, 2);

        Assert.True(themed.IsThemed);
        Assert.Equal(1, themed.Light);
        Assert.Equal(2, themed.Dark);
    }

    [Fact]
    public void ImplementsIThemedValue_ViaBoxing()
    {
        object boxed = new Themed<int>(1, 2);

        var themed = Assert.IsAssignableFrom<IThemedValue>(boxed);
        Assert.True(themed.IsThemed);
        Assert.Equal(1, themed.LightValue);
        Assert.Equal(2, themed.DarkValue);
    }
}

public class ThemeResourceKeyTests
{
    private class FakeTheme
    {
        public Dictionary<string, IBrush> Colors { get; init; } = [];
        public Nested Sub { get; init; } = new();
    }

    private class Nested
    {
        public IBrush? Accent { get; init; }
    }

    [AvaloniaFact]
    public void MemberAccess_DerivesDottedPath()
    {
        var resource = new StyleValue.Resource(Border.BackgroundProperty, (FakeTheme t) => t.Sub.Accent!);

        Assert.Equal("Sub.Accent", resource.Key);
    }

    [AvaloniaFact]
    public void IndexerAccess_DerivesKeyFromEvaluatedArgument()
    {
        var value = "red-500";
        var resource = new StyleValue.Resource(Border.BackgroundProperty, (FakeTheme t) => t.Colors[value]);

        Assert.Equal("Colors[red-500]", resource.Key);
    }

    [AvaloniaFact]
    public void TwoRulesReadingTheSameThemeEntry_AgreeOnTheSameKey()
    {
        var value = "red-500";
        var first = new StyleValue.Resource(Border.BackgroundProperty, (FakeTheme t) => t.Colors[value]);
        var second = new StyleValue.Resource(Border.BorderBrushProperty, (FakeTheme t) => t.Colors[value]);

        Assert.Equal(first.Key, second.Key);
    }

    [AvaloniaFact]
    public void DifferentThemeEntries_NeverCollideOnKey()
    {
        var first = new StyleValue.Resource(Border.BackgroundProperty, (FakeTheme t) => t.Colors["red-500"]);
        var second = new StyleValue.Resource(Border.BackgroundProperty, (FakeTheme t) => t.Colors["blue-500"]);

        Assert.NotEqual(first.Key, second.Key);
    }
}

public class ResourceDictionaryEmitterTests
{
    private class FakeTheme
    {
        public Themed<IBrush> Plain { get; init; } = SolidColorBrush.Parse("#ff0000");
        public Themed<IBrush> ThemedBrush { get; init; } = new(SolidColorBrush.Parse("#ffffff"), SolidColorBrush.Parse("#000000"));
    }

    [AvaloniaFact]
    public void Emit_CompilesAndBuildsAResourceDictionary_WithGlobalAndThemedEntries()
    {
        var theme = new FakeTheme();
        var property = Border.BackgroundProperty;

        Expression<Func<FakeTheme, object>> plainAccess = t => t.Plain;
        Expression<Func<FakeTheme, object>> themedAccess = t => t.ThemedBrush;

        var plain = new StyleValue.Resource(property, plainAccess);
        var themed = new StyleValue.Resource(property, themedAccess);
        Assert.Equal("Plain", plain.Key);
        Assert.Equal("ThemedBrush", themed.Key);

        var resources = new Dictionary<string, StyleValue.Resource> { [plain.Key] = plain, [themed.Key] = themed };

        var emitted = ResourceDictionaryEmitter.Emit(resources, theme, "GeneratedResources", "AtomicResources");

        var (assembly, errors) = Compile(emitted);
        Assert.True(errors.Length == 0, string.Join('\n', errors.Select(e => e.ToString())) + "\n\n" + emitted);

        var type = assembly!.GetType("GeneratedResources.AtomicResources")!;
        var dictionary = (ResourceDictionary)type.GetMethod("Build")!.Invoke(null, null)!;

        Assert.True(dictionary.TryGetResource("Plain", null, out var plainValue));
        Assert.Equal(Color.Parse("#ff0000"), Assert.IsType<SolidColorBrush>(plainValue).Color);

        Assert.Equal(2, dictionary.ThemeDictionaries.Count);
        var light = (IResourceDictionary)dictionary.ThemeDictionaries[ThemeVariant.Light];
        var dark = (IResourceDictionary)dictionary.ThemeDictionaries[ThemeVariant.Dark];
        Assert.Equal(Color.Parse("#ffffff"), Assert.IsType<SolidColorBrush>(light["ThemedBrush"]).Color);
        Assert.Equal(Color.Parse("#000000"), Assert.IsType<SolidColorBrush>(dark["ThemedBrush"]).Color);
    }

    private static (System.Reflection.Assembly? Assembly, Diagnostic[] Errors) Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "GeneratedResourcesAssembly",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
            return (null, errors);

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
            return (null, [.. result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)]);

        stream.Seek(0, SeekOrigin.Begin);
        return (System.Reflection.Assembly.Load(stream.ToArray()), []);
    }
}
