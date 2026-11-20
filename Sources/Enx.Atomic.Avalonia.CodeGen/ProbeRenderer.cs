using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// Renders a <see cref="ProbeReport"/> either as Spectre output for a human or as the stable JSON document
/// documented on <see cref="ProbeSettings"/>. Both renderers write through the injected
/// <see cref="IAnsiConsole"/> so an embedding host can capture the exact bytes.
/// </summary>
internal static class ProbeRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Probe output is consumed by agents, not embedded in HTML: keep "<Control>" and similar readable
        // instead of paying for \u003C escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Writes the machine-readable document. Field names are pinned on the DTOs, not derived from C# names.</summary>
    public static void RenderJson(ProbeReport report, IAnsiConsole console)
    {
        // Write straight to the profile's writer: console.Write(string) runs the text through Spectre's
        // renderer, which word-wraps at the console width and would corrupt a long JSON string.
        var writer = console.Profile.Out.Writer;
        writer.Write(JsonSerializer.Serialize(report, JsonOptions));
        writer.Write(Environment.NewLine);
        writer.Flush();
    }

    /// <summary>Writes the human-readable report: one block per token, followed by resources and the control experiment.</summary>
    public static void Render(ProbeReport report, IAnsiConsole console)
    {
        var styleCount = report.Tokens.Sum(token => token.Styles.Count);
        console.MarkupLineInterpolated(
            $"[bold]probe[/]: {report.Tokens.Count} token(s), {styleCount} style(s) in emission order"
        );

        foreach (var token in report.Tokens)
            RenderToken(token, console);

        if (report.Resources.Count > 0)
        {
            console.WriteLine();
            console.MarkupLine("[bold]resources[/]");
            foreach (var resource in report.Resources)
            {
                var themed = resource.Themed is null ? "not resolved" : resource.Themed.Value.ToString();
                console.WriteLine($"  {resource.Key} (themed: {themed})");
            }
        }

        if (report.Control is { } control)
        {
            console.WriteLine();
            var pseudo = control.PseudoClasses.Count == 0 ? "(none)" : string.Join(" ", control.PseudoClasses);
            console.MarkupLineInterpolated(
                $"[bold]control[/]: {Markup.Escape(control.Type)} (width {ProbeValues.Invariant(control.Width)}, pseudo {pseudo})"
            );
            console.WriteLine($"  resource dictionary: {control.ResourceCount} entry(ies)");
            console.MarkupLine("  effective values:");
            foreach (var value in control.EffectiveValues)
            {
                var line = $"    {value.Property} = {value.Value} ({SimpleTypeName(value.ValueType)})";
                if (value.ComposedFrom is { Count: > 0 })
                    line += $" [composed from {string.Join(", ", value.ComposedFrom)}]";
                if (value.ResourceKey is not null)
                    line += $" [resource {value.ResourceKey}]";
                console.WriteLine(line);
            }
        }
    }

    private static void RenderToken(ProbeTokenReport token, IAnsiConsole console)
    {
        console.WriteLine();
        console.MarkupLineInterpolated($"[bold]{Markup.Escape(token.Raw)}[/]");

        var variants =
            token.Variants.Count == 0
                ? "(none)"
                : string.Join(
                    ", ",
                    token.Variants.Select(variant =>
                        $"{(variant.Text is null ? "?" : variant.Text)} ({variant.Variant})"
                    )
                );
        console.WriteLine($"  variants: {variants}");
        console.WriteLine($"  base:     {token.BaseToken}");

        if (!token.Matched)
        {
            console.MarkupLine("  rule:     [red]no rule matched[/]");
            foreach (var hint in token.Hints)
                console.WriteLine($"  hint:     {hint}");
            return;
        }

        console.WriteLine($"  rule:     {token.Rule} (index {token.RuleIndex}, rank {token.Rank})");

        foreach (var style in token.Styles)
        {
            console.WriteLine();
            console.WriteLine($"  style {style.Position}/{style.PositionCount}  {style.Selector}");
            console.WriteLine(
                $"    key:       specificity {style.SortKey.ClassCount} class(es)/{style.SortKey.TypeCount} type(s), "
                    + $"container {style.SortKey.ContainerGroup}/{ProbeValues.Invariant(style.SortKey.ContainerOrder)}, "
                    + $"rule {style.SortKey.RuleIndex}, rank {style.SortKey.Rank}, sort {style.SortKey.Sort}"
            );
            if (style.ContainerQuery is not null)
                console.WriteLine($"    container: {style.ContainerQuery}");

            if (style.Setters.Count > 0)
            {
                console.WriteLine("    setters:");
                foreach (var setter in style.Setters)
                    console.WriteLine($"      {setter.Property} = {setter.Value} ({SimpleTypeName(setter.ValueType)})");
            }
        }
    }

    /// <summary>Trims a CLR type name to its last segment so the human output stays readable; the JSON keeps the full name.</summary>
    private static string SimpleTypeName(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return "null";

        var lastDot = fullName.LastIndexOf('.');
        return lastDot < 0 ? fullName : fullName[(lastDot + 1)..];
    }
}
