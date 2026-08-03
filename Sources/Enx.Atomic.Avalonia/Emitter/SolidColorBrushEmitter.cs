using Avalonia.Media;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Emits <see cref="SolidColorBrush"/> values as a <c>new SolidColorBrush(...)</c> local variable declaration,
/// since brushes are reference types and can't be expressed as a single inline literal.
/// </summary>
public class SolidColorBrushEmitter : ValueEmitter<SolidColorBrush>
{
    /// <inheritdoc/>
    public override IEnumerable<string> GetUsings()
    {
        yield return "Avalonia.Media";
    }

    /// <inheritdoc/>
    public override string ToCSharpString(SolidColorBrush value, out string? varName)
    {
        var name = $"brush{value.Color.R:X2}{value.Color.G:X2}{value.Color.B:X2}";
        varName = name;
        return $"var {name} = new SolidColorBrush(Color.Parse(\"#{value.Color.R:X2}{value.Color.G:X2}{value.Color.B:X2}\"));";
    }
}
