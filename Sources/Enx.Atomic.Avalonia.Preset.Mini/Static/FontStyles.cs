using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules setting <see cref="TextElement.FontStyleProperty"/>.</summary>
public static class FontStyles
{
    public static readonly Rule.Static[] All =
    [
        new("italic", [TextElement.FontStyleProperty.ToLiteral(FontStyle.Italic)]),
        new("not-italic", [TextElement.FontStyleProperty.ToLiteral(FontStyle.Normal)]),
        new("oblique", [TextElement.FontStyleProperty.ToLiteral(FontStyle.Oblique)]),
    ];
}
