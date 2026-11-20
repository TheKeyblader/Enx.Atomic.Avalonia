using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.CodeGen;
using Enx.Atomic.Avalonia.Compact;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// The multi-source half of <c>AtomicCli</c>: tokens and transformer-registered utilities are collected across every
/// source file and resolved once, so a class used by two files is emitted once. <see
/// cref="KnownBugsRegressionTests.SameTokenInTwoSourceFiles_IsEmittedOnce"/> covers the token case; these cover what
/// has no token to key on — an <c>ISourceTransformer</c> that registers a style through <c>AddUtil</c>.
/// </summary>
public class AtomicCliTests
{
    [AvaloniaFact]
    public void TransformerAddUtil_IsEmittedOnceEvenWhenItRunsPerFile()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();
        var transformer = new FixedUtilTransformer();
        configuration.Transformers.Add(transformer);

        var directory = Path.Combine(Path.GetTempPath(), $"enx-atomic-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var first = Path.Combine(directory, "First.axaml");
            var second = Path.Combine(directory, "Second.axaml");
            var output = Path.Combine(directory, "AtomicStyles.g.cs");
            File.WriteAllText(first, "<Border Classes=\"w-4\" />");
            File.WriteAllText(second, "<Border Classes=\"w-8\" />");

            var exitCode = AtomicCli.Run(["--output", output, first, second], configuration);
            Assert.Equal(0, exitCode);

            // Transformers still run once per file, with the file path as id.
            Assert.Equal(new[] { first, second }, transformer.SeenIds);

            var emitted = File.ReadAllText(output);

            // Two distinct width utilities plus one transformer style, not two.
            Assert.Equal(3, CountOccurrences(emitted, "new Style("));
            Assert.Equal(1, CountOccurrences(emitted, ".Class(\"from-transformer\")"));
            Assert.Equal(1, CountOccurrences(emitted, ".Class(\"w-4\")"));
            Assert.Equal(1, CountOccurrences(emitted, ".Class(\"w-8\")"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

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

/// <summary>
/// Registers one fixed style through <see cref="AtomicGenerator{TTheme}.AddUtil"/> on every file it sees — the
/// multi-file duplicate the CLI has to collapse structurally, since there is no token to deduplicate by.
/// </summary>
file sealed class FixedUtilTransformer : ISourceTransformer<MiniTheme>
{
    public List<string> SeenIds { get; } = [];
    public string Name => "fixed-util";
    public SourceTransformerEnforce Enforce => SourceTransformerEnforce.Default;
    public Func<string, bool>? IdFilter => null;
    public Func<string, string?, bool>? CodeFilter => null;

    public string Transform(string code, string? id, AtomicGenerator<MiniTheme> generator)
    {
        if (id is not null)
            SeenIds.Add(id);

        var selectorData = SelectorsExpression.Is<Control>(null).Class("from-transformer");
        var parameter = Expression.Parameter(typeof(Selector), "selector");
        generator.AddUtil(
            new StringifiedUtil
            {
                Index = 0,
                Selector = Expression.Lambda<Func<Selector, Selector>>(selectorData.ToExpression(parameter), parameter),
                SelectorData = selectorData,
                Body = [new Setter(Layoutable.WidthProperty, 10.0)],
                Metadata = new RuleMetadata(),
            }
        );

        return code;
    }
}
