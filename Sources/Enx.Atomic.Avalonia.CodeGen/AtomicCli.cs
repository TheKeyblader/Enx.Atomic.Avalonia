using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// A reusable CLI entry point for a user's configuration project (see the "Build-time C# code generation"
/// section of <c>ARCHITECTURE.md</c>): reads the given source files, resolves every utility token found in
/// them against a supplied <see cref="AtomicConfiguration{TTheme}"/>, and writes the emitted C# to disk.
/// A configuration project references this and calls <see cref="Run{TTheme}"/> from its own <c>Main</c>,
/// building its own <see cref="AtomicConfiguration{TTheme}"/> however it likes — this class only owns the
/// command-line contract (via Spectre.Console.Cli) and the read/resolve/emit/write orchestration.
///
/// <para>
/// Two commands are registered: <c>generate</c> (the default, so the MSBuild <c>.targets</c> invocation
/// <c>dotnet exec Config.dll --output ... files...</c> keeps working unchanged) and <c>probe</c> (a diagnostic
/// command that explains how a set of tokens resolves — see <see cref="ProbeSettings"/>).
/// </para>
/// </summary>
public static class AtomicCli
{
    /// <summary>
    /// Parses <paramref name="args"/> and runs the matching command: the default <c>generate</c> command, or
    /// <c>probe</c> when it is the first argument.
    /// </summary>
    /// <param name="args">The raw process arguments.</param>
    /// <param name="configuration">The configuration generated styles are resolved against.</param>
    /// <param name="console">
    /// The console to write to. Omit to use <see cref="AnsiConsole.Console"/>; an embedding host (a test, or
    /// another tool driving the CLI in-process) can supply its own to capture the output.
    /// </param>
    /// <returns>The process exit code Spectre.Console.Cli produces: <c>0</c> on success, non-zero on a
    /// parse/validation failure or an unhandled exception from a command.</returns>
    public static int Run<TTheme>(
        string[] args,
        AtomicConfiguration<TTheme> configuration,
        IAnsiConsole? console = null
    )
        where TTheme : class
    {
        // No custom ITypeRegistrar: CommandApp falls back to plain Activator.CreateInstance for the commands,
        // which needs a parameterless constructor — so the configuration and console are handed to them via a
        // static holder instead of constructor injection. Safe because this is always a single, synchronous,
        // single-shot CLI invocation (one Run call per process).
        CliState<TTheme>.Configuration = configuration;
        CliState<TTheme>.Console = console ?? AnsiConsole.Console;

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("enx-atomic");
            config.AddCommand<GenerateCommand<TTheme>>("generate");
            config.AddCommand<ProbeCommand<TTheme>>("probe");
        });

        // The default command keeps the MSBuild .targets invocation working: `dotnet exec Config.dll --output ...`
        // has no command name, so Spectre routes it here unchanged.
        app.SetDefaultCommand<GenerateCommand<TTheme>>();

        return app.Run(args);
    }
}

/// <summary>Shared, single-shot state for the commands Spectre instantiates without constructor arguments.</summary>
internal static class CliState<TTheme>
    where TTheme : class
{
    public static AtomicConfiguration<TTheme>? Configuration { get; set; }

    public static IAnsiConsole Console { get; set; } = AnsiConsole.Console;

    public static AtomicConfiguration<TTheme> RequireConfiguration(string command) =>
        Configuration
        ?? throw new InvalidOperationException(
            $"{command}'s configuration was not set before running the command — call it through {nameof(AtomicCli)}.{nameof(AtomicCli.Run)}."
        );
}

public sealed class AtomicCliSettings : CommandSettings
{
    [CommandOption("-o|--output <PATH>")]
    [Description("Path to write the generated .g.cs file to.")]
    public string Output { get; init; } = string.Empty;

    [CommandOption("-n|--namespace <NAME>")]
    [Description("Namespace of the generated Styles class. Defaults to 'GeneratedStyles'.")]
    public string Namespace { get; init; } = "GeneratedStyles";

    [CommandOption("-c|--class <NAME>")]
    [Description("Name of the generated Styles class. Defaults to 'AtomicStyles'.")]
    public string ClassName { get; init; } = "AtomicStyles";

    [CommandOption("--container <NAME>")]
    [Description("Container query name passed to StyleEmitter.Emit. Defaults to 'top-level'.")]
    public string ContainerName { get; init; } = "top-level";

    [CommandOption("--resources-output <PATH>")]
    [Description("Path to write the generated resource-dictionary .g.cs file to. Omit to skip resource generation.")]
    public string? ResourcesOutput { get; init; }

    [CommandOption("--resources-class <NAME>")]
    [Description("Name of the generated resource-dictionary class. Defaults to 'AtomicResources'.")]
    public string ResourcesClassName { get; init; } = "AtomicResources";

    /// <summary>Lets a caller request utilities not found in source without modifying the configuration project.</summary>
    [CommandOption("--safelist <TOKEN>")]
    [Description(
        "Extra utility tokens to generate in addition to the configured safelist. Repeatable; accepts whitespace-separated tokens."
    )]
    public string[] Safelist { get; init; } = [];

    [CommandArgument(0, "[SOURCES]")]
    [Description("Source files (XAML/C#) to scan for utility tokens.")]
    public string[] Sources { get; init; } = [];

    public override ValidationResult Validate() =>
        string.IsNullOrWhiteSpace(Output)
            ? ValidationResult.Error("--output is required.")
            : ValidationResult.Success();
}

internal sealed class GenerateCommand<TTheme> : Command<AtomicCliSettings>
    where TTheme : class
{
    public override int Execute(CommandContext context, AtomicCliSettings settings, CancellationToken cancellationToken)
    {
        var configuration = CliState<TTheme>.RequireConfiguration(nameof(GenerateCommand<TTheme>));
        var console = CliState<TTheme>.Console;

        var generator = new AtomicGenerator<TTheme>(configuration);
        var tokens = new HashSet<string>();
        var pendingUtils = new List<StringifiedUtil>();

        foreach (var source in settings.Sources)
        {
            if (!File.Exists(source))
            {
                console.MarkupLineInterpolated(
                    $"[yellow]Enx.Atomic.Avalonia.CodeGen: source file not found, skipping: '{source}'.[/]"
                );
                continue;
            }

            // Transformers still run per file (Id = the file path, so an IdFilter can scope one to a given
            // source), but extraction accumulates into one shared token set: resolving per file and
            // concatenating emitted a token used by two files twice. A transformer's AddUtil output has no
            // token to key on, so it is collected per file and deduplicated structurally at the end.
            var content = File.ReadAllText(source);
            var transformed = generator.ApplyTransformers(content, source);
            generator.ApplyExtractors(transformed, source, tokens);
            pendingUtils.AddRange(generator.PendingUtils);
        }

        // Resolve the union of all extracted and safelisted tokens, then deduplicate and sort together with
        // transformer-registered styles. Sorting the concatenation — not each file's slice — keeps a variant
        // after its base utility even when they come from different sources (see StyleOrdering).
        var utils = new List<StringifiedUtil>(generator.Generate(tokens, new() { Safelist = settings.Safelist }));

        // Unmatched shortcut expansions and safelist entries can be configuration typos, not build failures:
        // report them like the missing-source warning above and keep going.
        foreach (var diagnostic in generator.Diagnostics)
            console.MarkupLineInterpolated($"[yellow]Enx.Atomic.Avalonia.CodeGen: {diagnostic}.[/]");

        utils.AddRange(StyleOrdering.Distinct(pendingUtils));
        var ordered = StyleOrdering.Sort(utils);

        var emitted = StyleEmitter.Emit(ordered, settings.Namespace, settings.ClassName, settings.ContainerName);
        WriteIfDifferent(
            console,
            settings.Output,
            emitted,
            $"{utils.Count} style(s) from {settings.Sources.Length} source file(s)"
        );

        if (!string.IsNullOrWhiteSpace(settings.ResourcesOutput))
        {
            var resourcesEmitted = ResourceDictionaryEmitter.Emit(
                generator.ResolvedResources,
                configuration.Theme,
                settings.Namespace,
                settings.ResourcesClassName
            );
            WriteIfDifferent(
                console,
                settings.ResourcesOutput,
                resourcesEmitted,
                $"{generator.ResolvedResources.Count} resource(s)"
            );
        }

        return 0;
    }

    // Write-if-different, not an unconditional write: this CLI runs on every build (see the .targets file —
    // MSBuild's own Inputs/Outputs staleness check isn't reliable here, since a nested config-project build
    // can touch its output assembly's timestamp for reasons unrelated to any actual rule/theme change), so
    // leaving the output's mtime untouched when nothing actually changed is what lets the *consuming*
    // project's own incremental compile still skip recompiling the generated file.
    private static void WriteIfDifferent(IAnsiConsole console, string path, string content, string what)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(path) && File.ReadAllText(path) == content)
        {
            console.MarkupLineInterpolated($"Enx.Atomic.Avalonia.CodeGen: {what} unchanged at '{path}'.");
            return;
        }

        File.WriteAllText(path, content);
        console.MarkupLineInterpolated($"Enx.Atomic.Avalonia.CodeGen: wrote {what} to '{path}'.");
    }
}
