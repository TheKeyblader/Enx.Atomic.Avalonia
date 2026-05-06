using Avalonia.Media;

namespace Enx.Atomic.Avalonia;

public class SolidColorBrushEmitter : ValueEmitter<SolidColorBrush>
{
    public override IEnumerable<string> GetUsings()
    {
        yield return "Avalonia.Media";
    }

    public override string ToCSharpString(SolidColorBrush value, out string? varName)
    {
        var name = $"brush{value.Color.R:X2}{value.Color.G:X2}{value.Color.B:X2}";
        varName = name;
        return $"var {name} = new SolidColorBrush(Color.Parse(\"#{value.Color.R:X2}{value.Color.G:X2}{value.Color.B:X2}\"));";
    }
}
