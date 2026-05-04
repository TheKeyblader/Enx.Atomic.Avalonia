namespace Enx.Atomic.Avalonia;

internal record ParsedUtil
{
    public required int Index { get; init; }
    public required string Raw { get; init; }
    public required StyleValue[] StyleEntries { get; init; }
    public required RuleMetadata Metadata { get; init; }
}