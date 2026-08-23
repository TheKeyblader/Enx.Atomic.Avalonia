using Avalonia;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

public sealed class CornerRadiusValueEmitter : IValueEmitter
{
    public bool CanHandle(Type type) => type == typeof(CornerRadius);

    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia"];

    public string Emit(object value)
    {
        var r = (CornerRadius)value;
        return $"new CornerRadius({CSharpLiteral.Double(r.TopLeft)}, {CSharpLiteral.Double(r.TopRight)}, {CSharpLiteral.Double(r.BottomRight)}, {CSharpLiteral.Double(r.BottomLeft)})";
    }
}
