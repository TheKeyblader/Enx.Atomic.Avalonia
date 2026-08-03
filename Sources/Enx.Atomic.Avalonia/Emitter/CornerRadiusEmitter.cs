using Avalonia;

namespace Enx.Atomic.Avalonia;

/// <summary>Emits <see cref="CornerRadius"/> values as <c>new CornerRadius(...)</c> constructor calls.</summary>
public class CornerRadiusEmitter : ValueEmitter<CornerRadius>
{
    /// <inheritdoc/>
    public override IEnumerable<string> GetUsings()
    {
        yield return "Avalonia";
    }

    /// <inheritdoc/>
    public override string ToCSharpString(CornerRadius value, out string? varName)
    {
        varName = null;
        return $"new CornerRadius({value.TopLeft},{value.TopRight},{value.BottomRight},{value.BottomLeft})";
    }
}
