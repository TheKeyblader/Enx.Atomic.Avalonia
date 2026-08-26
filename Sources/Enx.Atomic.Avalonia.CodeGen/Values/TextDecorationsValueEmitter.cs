using Avalonia.Media;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>Emits the three named <see cref="TextDecorations"/> collections Preset.Mini's <c>TextDecoration</c> rule produces. A custom (non-referentially-equal) collection can't be reconstructed and throws.</summary>
public sealed class TextDecorationsValueEmitter : IValueEmitter
{
    /// <inheritdoc/>
    public bool CanHandle(Type type) => type == typeof(TextDecorationCollection);

    /// <inheritdoc/>
    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia.Media"];

    /// <inheritdoc/>
    public string Emit(object value) =>
        value switch
        {
            _ when ReferenceEquals(value, TextDecorations.Underline) => "TextDecorations.Underline",
            _ when ReferenceEquals(value, TextDecorations.Strikethrough) => "TextDecorations.Strikethrough",
            _ when ReferenceEquals(value, TextDecorations.Overline) => "TextDecorations.Overline",
            _ => throw new NotSupportedException("Cannot emit a custom TextDecorationCollection."),
        };
}
