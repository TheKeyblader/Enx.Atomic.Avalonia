namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>The container-query breakpoint scale (<c>sm:</c>, <c>md:</c>, ...), mirroring UnoCSS's <c>theme.breakpoints</c>.</summary>
public interface IBreakpointPart
{
    Dictionary<string, double> Breakpoints { get; set; }
}
