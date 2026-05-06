namespace Enx.Atomic.Avalonia;

public class ExtractorContext
{
    public required string RawCode { get; init; }
    public required string Code { get; set; }
    public string? Id { get; set; }
    public HashSet<string> Extracted { get; init; } = [];
}