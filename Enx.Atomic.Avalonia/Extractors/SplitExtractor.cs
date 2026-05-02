using System.Text.RegularExpressions;

namespace Enx.Atomic.Avalonia;

public partial class SplitExtractor : Extractor
{
    public override void Extract(ExtractorContext context)
    {
        var splits = SplitRegex().Split(context.Code);
        foreach (var token in splits)
            context.Extracted.Add(token);
    }

    [GeneratedRegex("""[\\:]?[\s'""`;{}]+""")]
    public static partial Regex SplitRegex();
}