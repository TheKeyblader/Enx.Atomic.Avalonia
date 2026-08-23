using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

public interface IColorPart
{
    Dictionary<string, IBrush> Colors { get; set; }
}
