using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Enx.Atomic.Avalonia.CodeGen;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// Avalonia's conditional class syntax, <c>Classes.line-through="{Binding IsDone}"</c>: <see cref="SplitExtractor"/>
/// alone sees <c>Classes.line-through=</c> as one unmatchable candidate, so before <see cref="ClassesAttributeExtractor"/>
/// such a token silently got no style.
/// </summary>
public class ClassesAttributeExtractorTests
{
    private static HashSet<string> Extract(string code)
    {
        var context = new ExtractorContext { RawCode = code, Code = code };
        new ClassesAttributeExtractor().Extract(context);
        return context.Extracted;
    }

    [Fact]
    public void ExtractsTheClassNameOfEachConditionalClassAttribute()
    {
        var extracted = Extract(
            """
            <TextBlock Classes="truncate"
                       Classes.line-through="{Binding IsDone}"
                       Classes.opacity-50 = "{Binding IsDone}"
                       Classes.done="{Binding IsDone}" />
            """
        );

        Assert.Equal(["done", "line-through", "opacity-50"], extracted.Order());
    }

    [Theory]
    // `.` inside the name is ambiguous with the attribute syntax itself: no partial `p-0` token.
    [InlineData("<Border Classes.p-0.5=\"{Binding X}\" />")]
    // Not an attribute: a property/method access in code-behind.
    [InlineData("control.Classes.Add(\"line-through\");")]
    [InlineData("var c = control.Classes.Count;")]
    // A longer identifier merely ending in "Classes".
    [InlineData("<Foo PseudoClasses.line-through=\"x\" />")]
    public void IgnoresWhatIsNotAPlainConditionalClassAttribute(string code)
    {
        Assert.Empty(Extract(code));
    }

    [AvaloniaFact]
    public void IsPartOfTheDefaultExtractors()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var tokens = generator.ApplyExtractors("<TextBlock Classes.line-through=\"{Binding IsDone}\" />");

        Assert.Contains("line-through", tokens);
    }

    [AvaloniaFact]
    public void ConditionalClass_GetsAStyleThatFollowsTheToggle()
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();
        var utils = generator.Generate(
            "<TextBlock Classes.line-through=\"{Binding IsDone}\" Classes.opacity-50=\"{Binding IsDone}\" />",
            new AtomicGenerator<MiniTheme>.Options { Id = "Item.axaml" }
        );

        var text = new TextBlock { Text = "todo" };
        var window = utils.ShowWithStyles(text);
        try
        {
            Assert.Null(text.TextDecorations);
            Assert.Equal(1d, text.Opacity);

            // What `Classes.line-through="{Binding IsDone}"` compiles to once IsDone turns true.
            text.Classes.Set("line-through", true);
            text.Classes.Set("opacity-50", true);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(TextDecorations.Strikethrough, text.TextDecorations);
            Assert.Equal(0.5, text.Opacity);

            text.Classes.Set("line-through", false);
            text.Classes.Set("opacity-50", false);
            Dispatcher.UIThread.RunJobs();

            Assert.Null(text.TextDecorations);
            Assert.Equal(1d, text.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AtomicCli_EmitsAStyleForAConditionalClassInAnAxamlFile()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();
        var directory = Path.Combine(Path.GetTempPath(), $"enx-atomic-classes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var source = Path.Combine(directory, "Item.axaml");
            var output = Path.Combine(directory, "AtomicStyles.g.cs");
            File.WriteAllText(source, "<TextBlock Classes=\"truncate\" Classes.line-through=\"{Binding IsDone}\" />");

            Assert.Equal(0, AtomicCli.Run(["--output", output, source], configuration));

            var emitted = File.ReadAllText(output);
            Assert.Contains(".Class(\"line-through\")", emitted);
            Assert.Contains(".Class(\"truncate\")", emitted);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
