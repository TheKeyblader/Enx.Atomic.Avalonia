using Avalonia.Media;

namespace Enx.Atomic.Avalonia;

public class FontWeightEmitter : ValueEmitter<FontWeight>
{
    public override IEnumerable<string> GetUsings()
    {
        yield return "Avalonia.Media";
    }

    public override string ToCSharpString(FontWeight value, out string? varName)
    {
        varName = null;
        return $"FontWeight.{value}";
    }
}
