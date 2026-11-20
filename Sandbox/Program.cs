using Avalonia;
using Enx.Atomic.Avalonia;
using Enx.Atomic.Avalonia.CodeGen;
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
    Console.WriteLine(
        $"after re-running AddMiniTheme -> rules: {configuration.Rules.Count}, variants: {configuration.Variants.Count}"
    );
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
        new MarkerTransformer("wrong-id", SourceTransformerEnforce.Default) { IdFilter = id => id == "other.axaml" }
    );

    var generatorForTransformers = new AtomicGenerator<MiniTheme>(configuration);
    var transformed = generatorForTransformers.ApplyTransformers("<original>", "test.axaml");
    Console.WriteLine($"ApplyTransformers -> {transformed}");
    Console.WriteLine();

    configuration.Transformers.Clear();

    // Composite side properties: p-*/pl-*/pt-*, border-*/-t-*, rounded-*/rounded-tl-* all write their host's
    // side attached property (PaddingSides/BorderThicknessSides/CornerRadiusSides), and the host's class
    // handler recomposes the real Thickness/CornerRadius. Several utilities on the same element therefore compose
    // instead of replacing each other, and a variant stays its own style.
    var sidesGenerator = new AtomicGenerator<MiniTheme>(configuration);
    var sidesSource =
        "Classes=\"p-2 pl-4\"\n"
        + "Classes=\"border border-t-2\"\n"
        + "Classes=\"rounded-tl-lg rounded-tr-md\"\n"
        + "Classes=\"hover:pt-4\"";
    var sideResults = sidesGenerator.Generate(
        sidesSource,
        new AtomicGenerator<MiniTheme>.Options { Id = "test.axaml" }
    );

    Console.WriteLine($"Composite sides -> {sideResults.Length} result(s)");
    foreach (var result in sideResults)
    {
        var selector = result.Selector.Compile()(null!);
        Console.WriteLine($"    selector: {selector}");
        foreach (var setter in result.Body)
            Console.WriteLine($"        {setter.Property} = {setter.Value}");
    }
    Console.WriteLine();

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

    // StyleEmitter: emit a real, compilable .cs file for a small representative set of tokens. Composite side
    // utilities (ml-1 mr-2, p-4) each stay their own style; the emitter embeds the host classes they target.
    var codeGenGenerator = new AtomicGenerator<MiniTheme>(configuration);
    var codeGenSource = "Classes=\"ml-1 mr-2 hidden flex-row cursor-pointer no-underline hover:bg-red-500 sm:hidden\"";
    var codeGenUtils = codeGenGenerator.Generate(
        codeGenSource,
        new AtomicGenerator<MiniTheme>.Options { Id = "demo.axaml" }
    );
    var emitted = StyleEmitter.Emit(codeGenUtils, "GeneratedStyles", "AtomicStyles");
    Console.WriteLine();
    Console.WriteLine("=== StyleEmitter output ===");
    Console.WriteLine(emitted);
}

file sealed class MarkerTransformer(string marker, SourceTransformerEnforce enforce) : ISourceTransformer<MiniTheme>
{
    public string Name { get; } = $"marker:{marker}";
    public SourceTransformerEnforce Enforce { get; } = enforce;
    public Func<string, bool>? IdFilter { get; init; }
    public Func<string, string?, bool>? CodeFilter { get; init; }

    public string Transform(string code, string? id, AtomicGenerator<MiniTheme> generator) => $"{code}[{marker}]";
}
