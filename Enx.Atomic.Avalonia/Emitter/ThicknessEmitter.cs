using Avalonia;

namespace Enx.Atomic.Avalonia;

public class ThicknessEmitter : ValueEmitter<Thickness>
{
    public override IEnumerable<string> GetUsings()
    {
        yield return "Avalonia";
    }

    public override string ToCSharpString(Thickness value, out string? varName)
    {
        varName = null;
        return $"new Thickness({value.Left},{value.Top},{value.Right},{value.Bottom})";
    }
}
