using Avalonia.Input;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>
/// Emits a <see cref="Cursor"/> built from a <see cref="StandardCursorType"/>. <see cref="Cursor"/> doesn't
/// expose which <see cref="StandardCursorType"/> it was built from, but its <see cref="Cursor.ToString"/>
/// happens to return exactly that enum member's name for that constructor overload — the only one Preset.Mini's
/// rules use — so it's reconstructible from that. A cursor built any other way (e.g. from a bitmap) can't be
/// emitted and throws.
/// </summary>
public sealed class CursorValueEmitter : IValueEmitter
{
    /// <inheritdoc/>
    public bool CanHandle(Type type) => type == typeof(Cursor);

    /// <inheritdoc/>
    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia.Input"];

    /// <inheritdoc/>
    public string Emit(object value)
    {
        var name = value.ToString();
        if (name is null || !Enum.TryParse<StandardCursorType>(name, out _))
        {
            throw new NotSupportedException(
                $"Cannot emit cursor '{name}' — only cursors built from a StandardCursorType are supported."
            );
        }

        return $"new Cursor(StandardCursorType.{name})";
    }
}
