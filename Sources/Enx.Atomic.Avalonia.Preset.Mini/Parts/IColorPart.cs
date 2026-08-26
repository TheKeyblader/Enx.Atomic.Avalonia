using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

public interface IColorPart
{
    /// <summary>
    /// A color scale entry may be assigned a plain <see cref="IBrush"/> (same value for both theme variants)
    /// or an explicit <see cref="Themed{IBrush}"/> — see <see cref="Themed{T}"/>.
    /// </summary>
    Dictionary<string, Themed<IBrush>> Colors { get; set; }
}
