using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

public class SourceTransformerPipelineTests
{
    [AvaloniaFact]
    public void Transformers_RunPreThenDefaultThenPost_DeclarationOrderWithinAStage()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(new MarkerTransformer("post", SourceTransformerEnforce.Post));
        configuration.Transformers.Add(new MarkerTransformer("d1", SourceTransformerEnforce.Default));
        configuration.Transformers.Add(new MarkerTransformer("pre", SourceTransformerEnforce.Pre));
        configuration.Transformers.Add(new MarkerTransformer("d2", SourceTransformerEnforce.Default));

        var result = generator.ApplyTransformers("<original>");

        Assert.Equal("<original>[pre][d1][d2][post]", result);
    }

    [AvaloniaFact]
    public void CodeFilter_ReturningFalse_SkipsTheTransformer()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(
            new MarkerTransformer("skipped", SourceTransformerEnforce.Default) { CodeFilter = (_, _) => false }
        );

        var result = generator.ApplyTransformers("<original>");

        Assert.Equal("<original>", result);
    }

    [AvaloniaFact]
    public void IdFilter_ReturningFalse_SkipsTheTransformer()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(
            new MarkerTransformer("wrong-id", SourceTransformerEnforce.Default) { IdFilter = id => id == "other.axaml" }
        );

        var result = generator.ApplyTransformers("<original>", "test.axaml");

        Assert.Equal("<original>", result);
    }

    [AvaloniaFact]
    public void IdFilter_SetButNoIdProvided_SkipsTheTransformer()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Transformers.Add(
            new MarkerTransformer("has-filter", SourceTransformerEnforce.Default) { IdFilter = _ => true }
        );

        var result = generator.ApplyTransformers("<original>");

        Assert.Equal("<original>", result);
    }
}

file sealed class MarkerTransformer(string marker, SourceTransformerEnforce enforce) : ISourceTransformer<MiniTheme>
{
    public string Name { get; } = $"marker:{marker}";
    public SourceTransformerEnforce Enforce { get; } = enforce;
    public Func<string, bool>? IdFilter { get; init; }
    public Func<string, string?, bool>? CodeFilter { get; init; }

    public string Transform(string code, string? id, AtomicGenerator<MiniTheme> generator) => $"{code}[{marker}]";
}
