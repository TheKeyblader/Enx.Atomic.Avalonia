using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules setting <see cref="Inline.TextDecorationsProperty"/>.</summary>
public static class TextDecoration
{
    public static readonly Rule.Static[] All =
    [
        new("underline", [Inline.TextDecorationsProperty.ToLiteral(TextDecorations.Underline)]),
        new("line-through", [Inline.TextDecorationsProperty.ToLiteral(TextDecorations.Strikethrough)]),
        new("overline", [Inline.TextDecorationsProperty.ToLiteral(TextDecorations.Overline)]),
        new("no-underline", [Inline.TextDecorationsProperty.ToLiteral(null)]),
    ];
}
