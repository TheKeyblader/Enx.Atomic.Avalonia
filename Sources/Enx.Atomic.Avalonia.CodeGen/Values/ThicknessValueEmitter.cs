using Avalonia;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>Emits a <see cref="Thickness"/> as <c>new Thickness(left, top, right, bottom)</c>.</summary>
public sealed class ThicknessValueEmitter : IValueEmitter
{
    /// <inheritdoc/>
    public bool CanHandle(Type type) => type == typeof(Thickness);

    /// <inheritdoc/>
    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia"];

    /// <inheritdoc/>
    public string Emit(object value)
    {
        var t = (Thickness)value;
        return $"new Thickness({CSharpLiteral.Double(t.Left)}, {CSharpLiteral.Double(t.Top)}, {CSharpLiteral.Double(t.Right)}, {CSharpLiteral.Double(t.Bottom)})";
    }
}
