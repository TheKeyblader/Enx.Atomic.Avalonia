using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// Options for the <c>probe</c> command: resolve a set of utility tokens, explain how each matched and where it
/// landed in the emission order, and optionally apply the result to a real control.
///
/// <para>
/// Tokens come from the positional <c>TEXT</c> — scanned with the configuration's extractors, so
/// <c>probe 'Classes="hover:ml-4 p-4"'</c> works — and/or from each <c>--file</c>, also scanned with the
/// configured extractors.
/// </para>
///
/// <para>
/// <c>--json</c> prints a single stable document instead of the Spectre report. It has three top-level keys:
/// </para>
/// <list type="bullet">
/// <item><c>tokens</c> — one entry per distinct extracted token, in extraction order: <c>raw</c>,
/// <c>variants</c> (outermost first, each with <c>text</c>/<c>variant</c>/<c>handler</c>), <c>baseToken</c>,
/// <c>shortcut</c> (the leaf tokens a shortcut base expanded to, null when it isn't one),
/// <c>matched</c>, <c>rule</c>/<c>ruleIndex</c>/<c>rank</c> (null when nothing matched), <c>hints</c>, and
/// <c>styles</c>.</item>
/// <item><c>resources</c> — every resource key a resolved setter references, with <c>themed</c> (whether the
/// theme entry splits into Light/Dark dictionaries).</item>
/// <item><c>control</c> — present only with <c>--control</c>: the resolved type, <c>width</c>,
/// <c>pseudoClasses</c>, <c>resourceCount</c>, and <c>effectiveValues</c> (the value Avalonia ended up with for
/// every property the styles touched, plus each recomposed composite property with its <c>composedFrom</c>
/// sides).</item>
/// </list>
/// <para>
/// Each entry in <c>styles</c> carries <c>position</c>/<c>positionCount</c> (1-based, in
/// <see cref="StyleOrdering.Sort"/>'s order for the whole probed set), the <c>selector</c> and
/// <c>containerQuery</c> rendered from the data trees, the <c>sortKey</c> parts (<c>classCount</c>,
/// <c>typeCount</c>, <c>layer</c>, <c>containerGroup</c>, <c>containerOrder</c>, <c>ruleIndex</c>,
/// <c>rank</c>, <c>sort</c>, <c>selectorText</c>), and its <c>setters</c> (<c>property</c>, <c>value</c>,
/// <c>valueType</c>, <c>resourceKey</c>).
/// </para>
/// </summary>
internal sealed class ProbeSettings : CommandSettings
{
    [CommandArgument(0, "[TEXT]")]
    [Description("Source text to scan for utility tokens, e.g. 'hover:ml-4 p-4' or 'Classes=\"...\"'.")]
    public string? Text { get; init; }

    [CommandOption("--file <PATH>")]
    [Description("An additional source file to scan for tokens. Repeatable.")]
    public string[] Files { get; init; } = [];

    [CommandOption("--control <TYPE>")]
    [Description("Instantiate this Avalonia control (e.g. Button, Border, TextBlock) and report effective values.")]
    public string? Control { get; init; }

    [CommandOption("--width <PX>")]
    [Description("Window width used with --control. Defaults to 800.")]
    public double Width { get; init; } = 800;

    [CommandOption("--pseudo <PSEUDO>")]
    [Description("Pseudo-class to apply to the control, e.g. :pointerover. Repeatable.")]
    public string[] PseudoClasses { get; init; } = [];

    [CommandOption("--json")]
    [Description("Print the stable machine-readable document instead of the human-readable report.")]
    public bool Json { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Text) && Files.Length == 0)
            return ValidationResult.Error("Provide source text to scan or at least one --file.");

        return Width <= 0 ? ValidationResult.Error("--width must be greater than zero.") : ValidationResult.Success();
    }
}

/// <summary>
/// Implements <c>probe</c> — the feedback loop for humans and agents: what each token matched, how the styles
/// were ordered, and (with <c>--control</c>) what Avalonia actually applied. Always exits <c>0</c> when the probe
/// ran; an unmatched token is reported in the output, not as a failure, since probing is diagnostic.
/// </summary>
internal sealed class ProbeCommand<TTheme> : Command<ProbeSettings>
    where TTheme : class
{
    public override int Execute(CommandContext context, ProbeSettings settings, CancellationToken cancellationToken)
    {
        var configuration = CliState<TTheme>.RequireConfiguration(nameof(ProbeCommand<TTheme>));
        var console = CliState<TTheme>.Console;
        var report = ProbeRunner<TTheme>.Run(settings, configuration);

        if (settings.Json)
            ProbeRenderer.RenderJson(report, console);
        else
            ProbeRenderer.Render(report, console);

        return 0;
    }
}
