using Avalonia.Media;

namespace Enx.Atomic.Avalonia;

public class ColorEmitter : ValueEmitter<Color>
{
    public override IEnumerable<string> GetUsings()
    {
        yield return "Avalonia.Media";
    }

    public override string ToCSharpString(Color value, out string? varName)
    {
        varName = null;
        return $"Color.Parse(\"#{value.R:X2}{value.G:X2}{value.B:X2}\")";
    }
}
