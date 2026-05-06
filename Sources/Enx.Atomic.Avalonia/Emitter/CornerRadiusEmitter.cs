using Avalonia;

namespace Enx.Atomic.Avalonia;

public class CornerRadiusEmitter : ValueEmitter<CornerRadius>
{
    public override IEnumerable<string> GetUsings()
    {
        yield return "Avalonia";
    }

    public override string ToCSharpString(CornerRadius value, out string? varName)
    {
        varName = null;
        return $"new CornerRadius({value.TopLeft},{value.TopRight},{value.BottomRight},{value.BottomLeft})";
    }
}
