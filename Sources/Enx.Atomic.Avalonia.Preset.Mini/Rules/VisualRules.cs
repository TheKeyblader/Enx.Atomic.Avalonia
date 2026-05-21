using Avalonia;

namespace Enx.Atomic.Avalonia.Preset.Mini.Rules;

public class VisualRules<TTheme>
    where TTheme : MiniTheme
{
    public IRule[] Get() =>
        [
            // IsVisibleProperty
            new Rule.Static("visible", [Visual.IsVisibleProperty.ToLiteral(true)]),
            new Rule.Static("hidden", [Visual.IsVisibleProperty.ToLiteral(false)]),
            
            // OpacityProperty
            new Rule.Static("collapse", [Visual.OpacityProperty.ToLiteral(0)]),
        ];
}
