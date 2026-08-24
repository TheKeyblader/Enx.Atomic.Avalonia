using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>
/// Emits a <see cref="ColumnDefinitions"/>/<see cref="RowDefinitions"/> value as <c>new ColumnDefinitions("1*,1*,...")</c>.
/// Both types' <c>ToString()</c> already round-trips through their <c>string</c> constructor (<c>GridLength.ParseLengths</c>),
/// the same technique <see cref="CursorValueEmitter"/> uses for <c>Cursor</c>.
/// </summary>
public sealed class GridDefinitionsValueEmitter : IValueEmitter
{
    public bool CanHandle(Type type) => type == typeof(ColumnDefinitions) || type == typeof(RowDefinitions);

    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia.Controls"];

    public string Emit(object value) =>
        value switch
        {
            ColumnDefinitions columns => $"new ColumnDefinitions({CSharpLiteral.String(columns.ToString())})",
            RowDefinitions rows => $"new RowDefinitions({CSharpLiteral.String(rows.ToString())})",
            _ => throw new NotSupportedException($"GridDefinitionsValueEmitter cannot handle '{value.GetType()}'."),
        };
}
