using Avalonia.Media;

namespace Enx.Atomic.Avalonia;

/// <summary>Emits <see cref="FontWeight"/> values as <c>FontWeight.XXX</c> enum member references.</summary>
public class FontWeightEmitter : ValueEmitter<FontWeight>
{
    /// <inheritdoc/>
    public override IEnumerable<string> GetUsings()
    {
        yield return "Avalonia.Media";
    }

    /// <inheritdoc/>
    public override string ToCSharpString(FontWeight value, out string? varName)
    {
        varName = null;
        return $"FontWeight.{value}";
    }
}
