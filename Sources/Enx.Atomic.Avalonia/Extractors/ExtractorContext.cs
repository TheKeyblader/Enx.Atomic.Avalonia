namespace Enx.Atomic.Avalonia;

/// <summary>Shared state passed to every <see cref="Extractor"/> run over a piece of source text.</summary>
public class ExtractorContext
{
    /// <summary>The original, unmodified source text.</summary>
    public required string RawCode { get; init; }

    /// <summary>The text extractors scan; earlier extractors may rewrite this for later ones.</summary>
    public required string Code { get; set; }

    /// <summary>Optional identifier scoping extracted tokens to their source file.</summary>
    public string? Id { get; set; }

    /// <summary>The accumulated set of candidate tokens found so far.</summary>
    public HashSet<string> Extracted { get; init; } = [];
}
