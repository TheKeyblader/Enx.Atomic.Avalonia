using Avalonia;
using Enx.Atomic.Avalonia;

var configuration = new AtomicConfiguration<MiniTheme>
{
    Theme = new MiniTheme(),
    Rules =
    [
        new Rule<MiniTheme>.Static("hidden", [
            Visual.IsVisibleProperty.ToLiteral(false)
        ]),
        new Rule<MiniTheme>.Static("visible", [
            Visual.IsVisibleProperty.ToLiteral(true)
        ]),
        new Rule<MiniTheme>.Static("collapsed", [
            Visual.OpacityProperty.ToLiteral(0)
        ])
    ]
};


public class MiniTheme
{
}