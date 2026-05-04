using Avalonia.Styling;
using System.Linq.Expressions;

namespace Enx.Atomic.Avalonia;

public record StringifiedUtil<TTheme>
    where TTheme : class
{
    public required int Index { get; init; }
    public required Selector Selector { get; set; }
    public StyleQuery? ContainerQuery { get; set; }
    public ThemeVariant? ThemeVariant { get; set; }
    public required SetterBase[] Body { get; init; }
    public required RuleMetadata Metadata { get; init; }
    public required RuleContext<TTheme> Context { get; init; }
}
