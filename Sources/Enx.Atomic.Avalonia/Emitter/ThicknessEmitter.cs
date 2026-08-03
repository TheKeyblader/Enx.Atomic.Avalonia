using Avalonia;

namespace Enx.Atomic.Avalonia;

/// <summary>Emits <see cref="Thickness"/> values as <c>new Thickness(...)</c> constructor calls.</summary>
public class ThicknessEmitter : ValueEmitter<Thickness>
{
    /// <inheritdoc/>
    public override IEnumerable<string> GetUsings()
    {
        yield return "Avalonia";
    }

    /// <inheritdoc/>
    public override string ToCSharpString(Thickness value, out string? varName)
    {
        varName = null;
        return $"new Thickness({value.Left},{value.Top},{value.Right},{value.Bottom})";
    }
}
