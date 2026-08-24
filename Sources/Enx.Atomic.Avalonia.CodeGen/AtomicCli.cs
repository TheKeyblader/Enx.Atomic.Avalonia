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
/// </summary>
public static class AtomicCli
{
    /// <summary>
    /// Parses <paramref name="args"/>, resolves every source file's utility tokens against
    /// <paramref name="configuration"/>, and writes the emitted C# to the requested output path.
    /// </summary>
    /// <returns>The process exit code Spectre.Console.Cli produces: <c>0</c> on success, non-zero on a
    /// parse/validation failure or an unhandled exception from <see cref="GenerateCommand{TTheme}"/>.</returns>
    public static int Run<TTheme>(string[] args, AtomicConfiguration<TTheme> configuration)
        where TTheme : class
    {
        // No custom ITypeRegistrar: CommandApp<T> falls back to plain Activator.CreateInstance for the
        // command, which needs a parameterless constructor — so the configuration is handed to
        // GenerateCommand<TTheme> via a static field instead of constructor injection. Safe because this is
        // always a single, synchronous, single-shot CLI invocation (one Run call per process).
        GenerateCommand<TTheme>.Configuration = configuration;

        var app = new CommandApp<GenerateCommand<TTheme>>(null);
        app.Configure(config => config.SetApplicationName("enx-atomic"));
        return app.Run(args);
    }
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
    internal static AtomicConfiguration<TTheme>? Configuration { get; set; }

    protected override int Execute(CommandContext context, AtomicCliSettings settings, CancellationToken cancellationToken)
    {
        var configuration = Configuration ?? throw new InvalidOperationException(
            $"{nameof(GenerateCommand<TTheme>)}.{nameof(Configuration)} was not set before running the command — call it through {nameof(AtomicCli)}.{nameof(AtomicCli.Run)}."
        );

        var generator = new AtomicGenerator<TTheme>(configuration);
        var utils = new List<StringifiedUtil>();

        foreach (var source in settings.Sources)
        {
            if (!File.Exists(source))
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[yellow]Enx.Atomic.Avalonia.CodeGen: source file not found, skipping: '{source}'.[/]"
                );
                continue;
            }

            var content = File.ReadAllText(source);
            utils.AddRange(generator.Generate(content, new AtomicGenerator<TTheme>.Options { Id = source }));
        }

        var emitted = StyleEmitter.Emit(utils, settings.Namespace, settings.ClassName, settings.ContainerName);

        var outputDirectory = Path.GetDirectoryName(settings.Output);
        if (!string.IsNullOrEmpty(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        File.WriteAllText(settings.Output, emitted);
        AnsiConsole.MarkupLineInterpolated(
            $"Enx.Atomic.Avalonia.CodeGen: wrote {utils.Count} style(s) from {settings.Sources.Length} source file(s) to '{settings.Output}'."
        );
        return 0;
    }
}
