using Avalonia;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

public sealed class ThicknessValueEmitter : IValueEmitter
{
    public bool CanHandle(Type type) => type == typeof(Thickness);

    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia"];

    public string Emit(object value)
    {
        var t = (Thickness)value;
        return $"new Thickness({CSharpLiteral.Double(t.Left)}, {CSharpLiteral.Double(t.Top)}, {CSharpLiteral.Double(t.Right)}, {CSharpLiteral.Double(t.Bottom)})";
    }
}
