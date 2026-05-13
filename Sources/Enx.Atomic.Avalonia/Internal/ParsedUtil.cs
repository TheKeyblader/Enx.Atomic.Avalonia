namespace Enx.Atomic.Avalonia.Internal;

internal record ParsedUtil
{
    public required int Index { get; init; }
    public required string Raw { get; init; }
    public required StyleValue[] StyleEntries { get; init; }
    public required RuleMetadata Metadata { get; init; }
    public required VariantHandlerBase[] VariantHandlers { get; init; }
}
