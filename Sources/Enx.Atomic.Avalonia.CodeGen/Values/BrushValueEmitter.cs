using Avalonia.Media;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

public sealed class BrushValueEmitter : IValueEmitter
{
    public bool CanHandle(Type type) => typeof(ISolidColorBrush).IsAssignableFrom(type);

    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia.Media"];

    public string Emit(object value)
    {
        var color = ((ISolidColorBrush)value).Color;
        return $"new SolidColorBrush(Color.FromArgb({color.A}, {color.R}, {color.G}, {color.B}))";
    }
}
