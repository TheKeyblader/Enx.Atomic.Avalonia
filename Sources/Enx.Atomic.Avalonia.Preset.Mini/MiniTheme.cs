namespace Enx.Atomic.Avalonia.Preset.Mini;

public class MiniTheme
{
    public const string DefaultKey = "DEFAULT";
    public double RemToPxRatio { get; set; } = 16;

    public Dictionary<string, double> Spacing { get; set; } =
        new()
        {
            { DefaultKey, 0.25 },
            { "xs", 0.75 },
            { "sm", 0.875 },
            { "lg", 1.125 },
            { "xl", 1.25 },
            { "2xl", 1.50 },
            { "3xl", 1.875 },
            { "4xl", 2.25 },
            { "5xl", 3 },
            { "6xl", 3.75 },
            { "7xl", 4.5 },
            { "8xl", 6 },
            { "9xl", 8 },
        };

    public Dictionary<string, double> Radius { get; set; } =
        new()
        {
            { DefaultKey, 0.25 },
            { "none", 0 },
            { "xs", 0.125 },
            { "sm", 0.25 },
            { "md", 0.375 },
            { "lg", 0.5 },
            { "xl", 0.75 },
            { "2xl", 1 },
            { "3xl", 1.5 },
            { "4xl", 2 },
        };

    public Dictionary<string, double> TextSize { get; set; } =
        new()
        {
            { "xs", 0.75 },
            { "sm", 0.875 },
            { "base", 1 },
            { "lg", 1.125 },
            { "xl", 1.25 },
            { "2xl", 1.5 },
            { "3xl", 1.875 },
            { "4xl", 2.25 },
            { "5xl", 3 },
            { "6xl", 3.75 },
            { "7xl", 4.5 },
            { "8xl", 6 },
            { "9xl", 8 },
        };

    public Dictionary<string, double> TextLineHeight { get; set; } =
        new()
        {
            { "xs", 1 },
            { "sm", 1.25 },
            { "base", 1.5 },
            { "lg", 1.75 },
            { "xl", 1.75 },
            { "2xl", 2 },
            { "3xl", 2.25 },
            { "4xl", 2.5 },
            { "5xl", 1 },
            { "6xl", 1 },
            { "7xl", 1 },
            { "8xl", 1 },
            { "9xl", 1 },
        };

    public Dictionary<string, double> Tracking { get; set; } =
        new()
        {
            { "tighter", -0.05 },
            { "tight", -0.025 },
            { "normal", 0 },
            { "wide", 0.025 },
            { "wider", 0.05 },
            { "widest", 0.1 },
        };

    public Dictionary<string, double> Leading { get; set; } =
        new()
        {
            { "none", 1 },
            { "tight", 1.25 },
            { "snug", 1.375 },
            { "normal", 1.5 },
            { "relaxed", 1.625 },
            { "loose", 2 },
        };

    public Dictionary<string, double> Breakpoints { get; set; } =
        new()
        {
            { "sm", 640 },
            { "md", 768 },
            { "lg", 1024 },
            { "xl", 1280 },
            { "2xl", 1536 },
        };
}
