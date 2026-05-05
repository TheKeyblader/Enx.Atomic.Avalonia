using Avalonia.Styling;
using System.Linq.Expressions;

namespace Enx.Atomic.Avalonia;

public record StringifiedUtil<TTheme>
    where TTheme : class
{
    public required int Index { get; init; }
    public required Expression<Func<Selector, Selector>> Selector { get; set; }
    public Expression<Func<StyleQuery, StyleQuery>>? ContainerQuery { get; set; }
    public ThemeVariant? ThemeVariant { get; set; }
    public required Setter[] Body { get; init; }
    public required RuleMetadata Metadata { get; init; }
    public required RuleContext<TTheme> Context { get; init; }
}
