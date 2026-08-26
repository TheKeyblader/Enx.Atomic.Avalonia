using Avalonia.Media;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>Emits any <see cref="ISolidColorBrush"/> as <c>new SolidColorBrush(Color.FromArgb(...))</c>.</summary>
public sealed class BrushValueEmitter : IValueEmitter
{
    /// <inheritdoc/>
    public bool CanHandle(Type type) => typeof(ISolidColorBrush).IsAssignableFrom(type);

    /// <inheritdoc/>
    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia.Media"];

    /// <inheritdoc/>
    public string Emit(object value)
    {
        var color = ((ISolidColorBrush)value).Color;
        return $"new SolidColorBrush(Color.FromArgb({color.A}, {color.R}, {color.G}, {color.B}))";
    }
}
