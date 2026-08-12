using Avalonia.Input;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules setting <see cref="InputElement.CursorProperty"/> to a fixed <see cref="StandardCursorType"/>.</summary>
public static class Cursors
{
    public static readonly Rule.Static[] All =
    [
        new("cursor-default", [InputElement.CursorProperty.ToLiteral(new Cursor(StandardCursorType.Arrow))]),
        new("cursor-pointer", [InputElement.CursorProperty.ToLiteral(new Cursor(StandardCursorType.Hand))]),
        new("cursor-text", [InputElement.CursorProperty.ToLiteral(new Cursor(StandardCursorType.Ibeam))]),
        new("cursor-wait", [InputElement.CursorProperty.ToLiteral(new Cursor(StandardCursorType.Wait))]),
        new("cursor-move", [InputElement.CursorProperty.ToLiteral(new Cursor(StandardCursorType.SizeAll))]),
        new("cursor-not-allowed", [InputElement.CursorProperty.ToLiteral(new Cursor(StandardCursorType.No))]),
        new("cursor-crosshair", [InputElement.CursorProperty.ToLiteral(new Cursor(StandardCursorType.Cross))]),
        new("cursor-help", [InputElement.CursorProperty.ToLiteral(new Cursor(StandardCursorType.Help))]),
        new("cursor-none", [InputElement.CursorProperty.ToLiteral(new Cursor(StandardCursorType.None))]),
    ];
}
