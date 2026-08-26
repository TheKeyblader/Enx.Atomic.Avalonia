using Avalonia;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>Emits a <see cref="CornerRadius"/> as <c>new CornerRadius(topLeft, topRight, bottomRight, bottomLeft)</c>.</summary>
public sealed class CornerRadiusValueEmitter : IValueEmitter
{
    /// <inheritdoc/>
    public bool CanHandle(Type type) => type == typeof(CornerRadius);

    /// <inheritdoc/>
    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia"];

    /// <inheritdoc/>
    public string Emit(object value)
    {
        var r = (CornerRadius)value;
        return $"new CornerRadius({CSharpLiteral.Double(r.TopLeft)}, {CSharpLiteral.Double(r.TopRight)}, {CSharpLiteral.Double(r.BottomRight)}, {CSharpLiteral.Double(r.BottomLeft)})";
    }
}
