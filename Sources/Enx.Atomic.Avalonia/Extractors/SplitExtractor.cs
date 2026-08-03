using System.Text.RegularExpressions;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// The default <see cref="Extractor"/>: splits source text on whitespace and common markup delimiters
/// (quotes, braces, semicolons) to produce candidate tokens, with no attempt at validating them against
/// actual rules. Cheap and works well enough given that non-matching tokens are simply ignored downstream.
/// </summary>
public partial class SplitExtractor : Extractor
{
    /// <inheritdoc/>
    public override void Extract(ExtractorContext context)
    {
        var splits = SplitRegex().Split(context.Code);
        foreach (var token in splits)
            context.Extracted.Add(token);
    }

    /// <summary>Matches runs of whitespace or delimiter characters (optionally preceded by <c>\</c> or <c>:</c>) used as token boundaries.</summary>
    [GeneratedRegex("""[\\:]?[\s'""`;{}]+""")]
    public static partial Regex SplitRegex();
}