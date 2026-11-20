using Avalonia.Media;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>
/// The box-shadow scale (<c>shadow</c>, <c>shadow-md</c>, ...), mirroring UnoCSS's <c>theme.boxShadow</c> but
/// already resolved to Avalonia <see cref="BoxShadows"/> instances: the upstream scale is a list of CSS strings
/// (with a <c>var(--un-shadow-inset)</c> placeholder), while a shadow is fully known once the theme is built, so
/// there is no reason to re-parse a string per token.
/// <para>
/// A key's value may hold several layers (<see cref="BoxShadows"/> is a list) and an inset layer
/// (<see cref="BoxShadow.IsInset"/>). An empty <see cref="BoxShadows"/> (the default value) means no shadow —
/// <c>shadow-none</c> maps to it.
/// </para>
/// </summary>
public interface IBoxShadowPart
{
    Dictionary<string, BoxShadows> BoxShadows { get; set; }
}
