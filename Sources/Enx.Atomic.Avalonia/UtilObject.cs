using Avalonia.Styling;
using System.Linq.Expressions;

namespace Enx.Atomic.Avalonia;

internal record UtilObject
{
    public required Expression<Func<Selector, Selector>> Selector { get; set; }
    public Expression<Func<StyleQuery, StyleQuery>>? ContainerQuery { get; set; }
    public StyleValue[] Entries { get; init; } = [];
    public int Sort { get; init; }
}
