using System.Text.Json;
using Enx.Atomic.Avalonia.CodeGen;
using Spectre.Console;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// The <c>probe</c> command: resolving a token, reporting variant/rule/setter data and emission-order positions,
/// and (with <c>--control</c>) the effective values Avalonia applies. Output is captured through an injected
/// <see cref="IAnsiConsole"/>, which is also why <see cref="AtomicCli.Run{TTheme}"/> accepts one.
/// </summary>
public class ProbeCommandTests
{
    [AvaloniaFact]
    public void HoverMarginToken_ReportsVariantRuleSetterAndEffectiveMargin()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();
        var (console, writer) = CreateConsole();

        var exitCode = AtomicCli.Run(
            ["probe", "hover:ml-4", "--control", "TextBlock", "--pseudo", ":pointerover", "--json"],
            configuration,
            console
        );

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(writer.ToString());
        var token = document.RootElement.GetProperty("tokens")[0];

        Assert.Equal("hover:ml-4", token.GetProperty("raw").GetString());
        var variant = token.GetProperty("variants")[0];
        Assert.Equal("hover:", variant.GetProperty("text").GetString());
        Assert.Equal("PseudoClassVariant", variant.GetProperty("variant").GetString());
        Assert.Equal("ml-4", token.GetProperty("baseToken").GetString());
        Assert.True(token.GetProperty("matched").GetBoolean());
        Assert.Equal("MarginRule", token.GetProperty("rule").GetString());

        var setter = token.GetProperty("styles")[0].GetProperty("setters")[0];
        Assert.Equal("MarginSides.MarginLeftProperty", setter.GetProperty("property").GetString());
        Assert.Equal("16", setter.GetProperty("value").GetString());

        var control = document.RootElement.GetProperty("control");
        Assert.Equal("Avalonia.Controls.TextBlock", control.GetProperty("type").GetString());
        var margin = control
            .GetProperty("effectiveValues")
            .EnumerateArray()
            .Single(value => value.GetProperty("property").GetString() == "Layoutable.MarginProperty");
        Assert.Equal("16,0,0,0", margin.GetProperty("value").GetString());
        Assert.Contains(
            "MarginSides.MarginLeftProperty",
            margin.GetProperty("composedFrom").EnumerateArray().Select(x => x.GetString())
        );
    }

    [AvaloniaFact]
    public void UnknownToken_ReportsNoMatchAndAHint()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();
        var (console, writer) = CreateConsole();

        var exitCode = AtomicCli.Run(["probe", "nope-1", "--json"], configuration, console);

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(writer.ToString());
        var token = document.RootElement.GetProperty("tokens")[0];

        Assert.False(token.GetProperty("matched").GetBoolean());
        Assert.False(token.TryGetProperty("rule", out _));
        Assert.Empty(token.GetProperty("styles").EnumerateArray());
        Assert.NotEmpty(token.GetProperty("hints").EnumerateArray());
        Assert.False(document.RootElement.TryGetProperty("control", out _));
    }

    [AvaloniaFact]
    public void OrderingPositions_PlaceBaseUtilityBeforeItsHoverVariant()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();
        var (console, writer) = CreateConsole();

        var exitCode = AtomicCli.Run(["probe", "hover:w-8 w-4", "--json"], configuration, console);

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(writer.ToString());
        var tokens = document.RootElement.GetProperty("tokens").EnumerateArray().ToArray();

        var baseStyle = tokens.Single(token => token.GetProperty("raw").GetString() == "w-4").GetProperty("styles")[0];
        var hoverStyle = tokens
            .Single(token => token.GetProperty("raw").GetString() == "hover:w-8")
            .GetProperty("styles")[0];

        Assert.Equal(1, baseStyle.GetProperty("position").GetInt32());
        Assert.Equal(2, hoverStyle.GetProperty("position").GetInt32());
        Assert.True(
            hoverStyle.GetProperty("sortKey").GetProperty("classCount").GetInt32()
                > baseStyle.GetProperty("sortKey").GetProperty("classCount").GetInt32()
        );
    }

    [AvaloniaFact]
    public void TextArgument_IsScannedWithTheConfiguredExtractors()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();
        var (console, writer) = CreateConsole();

        var exitCode = AtomicCli.Run(["probe", "Classes=\"hover:ml-4 p-4\"", "--json"], configuration, console);

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(writer.ToString());
        var raws = document
            .RootElement.GetProperty("tokens")
            .EnumerateArray()
            .Select(t => t.GetProperty("raw").GetString())
            .ToArray();

        Assert.Contains("hover:ml-4", raws);
        Assert.Contains("p-4", raws);
    }

    [AvaloniaFact]
    public void HumanOutput_NamesTheRuleAndSetter()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();
        var (console, writer) = CreateConsole();

        var exitCode = AtomicCli.Run(["probe", "hover:ml-4"], configuration, console);

        Assert.Equal(0, exitCode);
        var output = writer.ToString();
        Assert.Contains("PseudoClassVariant", output);
        Assert.Contains("MarginRule", output);
        Assert.Contains("MarginSides.MarginLeftProperty = 16", output);
    }

    [AvaloniaFact]
    public void ShortcutToken_ReportsItsExpansionAndTheShortcutLayer()
    {
        var (configuration, _) = TestHelpers.CreateMiniGenerator();
        configuration.Shortcuts.Add(Shortcut.Static("btn", "opacity-50"));
        var (console, writer) = CreateConsole();

        var exitCode = AtomicCli.Run(["probe", "btn opacity-100", "--json"], configuration, console);

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(writer.ToString());
        var tokens = document.RootElement.GetProperty("tokens").EnumerateArray().ToArray();

        var shortcut = tokens.Single(token => token.GetProperty("raw").GetString() == "btn");
        var utility = tokens.Single(token => token.GetProperty("raw").GetString() == "opacity-100");

        Assert.Equal(
            new[] { "opacity-50" },
            shortcut.GetProperty("shortcut").EnumerateArray().Select(value => value.GetString())
        );
        Assert.False(utility.TryGetProperty("shortcut", out _));

        var shortcutStyle = shortcut.GetProperty("styles")[0];
        var utilityStyle = utility.GetProperty("styles")[0];
        Assert.Equal(StyleLayer.Shortcuts, shortcutStyle.GetProperty("sortKey").GetProperty("layer").GetInt32());
        Assert.Equal(StyleLayer.Default, utilityStyle.GetProperty("sortKey").GetProperty("layer").GetInt32());
        Assert.True(shortcutStyle.GetProperty("position").GetInt32() < utilityStyle.GetProperty("position").GetInt32());
    }

    private static (IAnsiConsole Console, StringWriter Writer) CreateConsole()
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(
            new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Interactive = InteractionSupport.No,
                Out = new AnsiConsoleOutput(writer),
            }
        );
        return (console, writer);
    }
}
