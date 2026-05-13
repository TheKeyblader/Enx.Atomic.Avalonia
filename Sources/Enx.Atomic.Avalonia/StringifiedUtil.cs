using Avalonia.Styling;
using System.Linq.Expressions;

namespace Enx.Atomic.Avalonia;

public record StringifiedUtil
{
    public required int Index { get; init; }
    public required Expression<Func<Selector, Selector>> Selector { get; set; }
    public Expression<Func<StyleQuery, StyleQuery>>? ContainerQuery { get; set; }
    public required Setter[] Body { get; init; }
    public required RuleMetadata Metadata { get; init; }
}
