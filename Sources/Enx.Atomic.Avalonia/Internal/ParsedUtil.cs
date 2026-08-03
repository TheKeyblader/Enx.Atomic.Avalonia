namespace Enx.Atomic.Avalonia.Internal;

/// <summary>
/// The result of resolving a token against a rule, before variant handlers have been applied to build the
/// final selector. An intermediate step between <see cref="Rule"/> matching and <see cref="StringifiedUtil"/> generation.
/// </summary>
internal record ParsedUtil
{
    /// <summary>The declaration order of the originating rule, for stable output ordering.</summary>
    public required int Index { get; init; }

    /// <summary>The original, unstripped token.</summary>
    public required string Raw { get; init; }

    /// <summary>The style values produced by the matched rule, all sharing the same property owner type.</summary>
    public required StyleValue[] StyleEntries { get; init; }

    /// <summary>Metadata from the rule that produced this result.</summary>
    public required RuleMetadata Metadata { get; init; }

    /// <summary>The variant handlers matched for this token, still to be applied.</summary>
    public required VariantHandlerBase[] VariantHandlers { get; init; }
}
