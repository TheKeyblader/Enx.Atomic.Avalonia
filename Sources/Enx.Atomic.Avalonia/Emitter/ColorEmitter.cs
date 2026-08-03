using Avalonia.Media;

namespace Enx.Atomic.Avalonia;

/// <summary>Emits <see cref="Color"/> values as <c>Color.Parse("#RRGGBB")</c> calls.</summary>
public class ColorEmitter : ValueEmitter<Color>
{
    /// <inheritdoc/>
    public override IEnumerable<string> GetUsings()
    {
        yield return "Avalonia.Media";
    }

    /// <inheritdoc/>
    public override string ToCSharpString(Color value, out string? varName)
    {
        varName = null;
        return $"Color.Parse(\"#{value.R:X2}{value.G:X2}{value.B:X2}\")";
    }
}
