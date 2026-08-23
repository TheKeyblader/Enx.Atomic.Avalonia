using System;
using Avalonia;
using Avalonia.Styling;
using Enx.Atomic.Avalonia;
using Enx.Atomic.Avalonia.Preset.Mini;

AppBuilder.Configure<Application>().UsePlatformDetect().Start(AppMain, args);

static void AppMain(Application app, string[] args)
{
    var builder = ThemeBuilder<MiniTheme>.Create();
    var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };
    builder.AddMiniTheme(configuration);

    Console.WriteLine($"rules: {configuration.Rules.Count}, variants: {configuration.Variants.Count}");
    Console.WriteLine(
        $"spacing: {configuration.Theme.Spacing.Count}, colors: {configuration.Theme.Colors.Count}, "
            + $"radii: {configuration.Theme.Radii.Count}, breakpoints: {configuration.Theme.Breakpoints.Count}"
    );
    Console.WriteLine();

    // calling twice should not double-register anything (AddRuleOnce/AddVariantOnce/AddStaticRuleOnce)
    builder.AddMiniTheme(configuration);
    Console.WriteLine($"after re-running AddMiniTheme -> rules: {configuration.Rules.Count}, variants: {configuration.Variants.Count}");
    Console.WriteLine();

    // ISourceTransformer pipeline: staging order (pre -> default, declaration order within a
    // stage -> post) and both filters (idFilter, codeFilter) should be respected.
    configuration.Transformers.Add(new MarkerTransformer("post", SourceTransformerEnforce.Post));
    configuration.Transformers.Add(new MarkerTransformer("d1", SourceTransformerEnforce.Default));
    configuration.Transformers.Add(new MarkerTransformer("pre", SourceTransformerEnforce.Pre));
    configuration.Transformers.Add(new MarkerTransformer("d2", SourceTransformerEnforce.Default));
    configuration.Transformers.Add(
        new MarkerTransformer("skipped", SourceTransformerEnforce.Default) { CodeFilter = (_, _) => false }
    );
    configuration.Transformers.Add(
        new MarkerTransformer("wrong-id", SourceTransformerEnforce.Default)
        {
            IdFilter = id => id == "other.axaml",
        }
    );

    var generatorForTransformers = new AtomicGenerator<MiniTheme>(configuration);
    var transformed = generatorForTransformers.ApplyTransformers("<original>", "test.axaml");
    Console.WriteLine($"ApplyTransformers -> {transformed}");
    Console.WriteLine();

    configuration.Transformers.Clear();

    // GhostPropertyCombiner: ml-1 + mr-2 on one line should combine into a single real Margin value;
    // mt-4 alone on another line should still fall back to its own (zero-elsewhere) style; and the
    // original ml-1/mr-2/mt-4 tokens themselves should resolve to nothing (SpecialProperties is filtered
    // out of emission), so only the synthesized combined tokens and p-2 should show up below.
    configuration.Transformers.Add(new GhostPropertyCombiner<MiniTheme>());
    var ghostGenerator = new AtomicGenerator<MiniTheme>(configuration);
    var ghostSource =
        "Classes=\"ml-1 mr-2\"\n"
        + "Classes=\"mt-4\"\n"
        + "Classes=\"p-2\"\n"
        + "Classes=\"pl-2 pt-4\"\n"
        + "Classes=\"rounded-tl-lg rounded-tr-md\"";
    Console.WriteLine("transformed:\n" + ghostGenerator.ApplyTransformers(ghostSource, "test.axaml"));
    Console.WriteLine($"rules after transform: {configuration.Rules.Count}");
    var ghostResults = ghostGenerator.Generate(ghostSource, new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" });

    Console.WriteLine($"Ghost combiner -> {ghostResults.Length} result(s)");
    foreach (var result in ghostResults)
    {
        var selector = result.Selector.Compile()(null!);
        Console.WriteLine($"    selector: {selector}");
        foreach (var setter in result.Body)
            Console.WriteLine($"        {setter.Property} = {setter.Value}");
    }
    Console.WriteLine();

    configuration.Transformers.Clear();
    var generator = new AtomicGenerator<MiniTheme>(configuration);

    string[] tokens =
    [
        // static rules
        "hidden",
        "cursor-pointer",
        "flex-row",
        "text-center",
        "italic",
        "font-bold",
        "truncate",
        "object-cover",
        // spacing (dictionary hit + negative)
        "m-4",
        "-m-4",
        "mx-8",
        "p-2",
        "gap-4",
        "gap-x-2",
        // spacing rem fallback (13 isn't in the default scale -> 13 * 16px)
        "m-13",
        // size / radius
        "w-sm",
        "min-h-lg",
        "rounded-lg",
        "rounded-t-xl",
        "rounded-full",
        // border width vs color ambiguity (both share the "border-" prefix)
        "border",
        "border-2",
        "border-red-500",
        // text size vs color ambiguity (both share the "text-" prefix)
        "text-sm",
        "text-red-500",
        // colors
        "bg-blue-500",
        // variants
        "hover:bg-red-500",
        "disabled:text-gray-500",
        "hover:focus:underline",
        "sm:flex-row",
        "max-sm:hidden",
        // should not match anything
        "not-a-real-class",
    ];

    foreach (var token in tokens)
    {
        var results = generator.ParseToken(token);
        Console.WriteLine($"{token} -> {results.Length} result(s)");

        foreach (var result in results)
        {
            var selector = result.Selector.Compile()(null!);
            Console.WriteLine($"    selector: {selector}");

            if (result.ContainerQuery is not null)
            {
                var query = result.ContainerQuery.Compile()(null!);
                Console.WriteLine($"    container: {query}");
            }

            foreach (var setter in result.Body)
                Console.WriteLine($"    {setter.Property} = {setter.Value}");
        }
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
