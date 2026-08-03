using Avalonia.Styling;
using System.Linq.Expressions;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// A fully resolved Avalonia style produced from a utility token: a selector expression, the property
/// setters it applies, and an optional container query, ready to be emitted by an <see cref="IStyleEmitter{TTheme}"/>.
/// </summary>
public record StringifiedUtil
{
    /// <summary>The declaration order of the originating rule, used to keep emitted styles deterministically ordered.</summary>
    public required int Index { get; init; }

    /// <summary>Expression building the Avalonia <see cref="Selector"/> this style applies to.</summary>
    public required Expression<Func<Selector, Selector>> Selector { get; set; }

    /// <summary>Expression building the container query to wrap this style in, or <see langword="null"/> if the style is not container-scoped.</summary>
    public Expression<Func<StyleQuery, StyleQuery>>? ContainerQuery { get; set; }

    /// <summary>The property setters applied by this style.</summary>
    public required Setter[] Body { get; init; }

    /// <summary>Metadata about the rule that produced this style.</summary>
    public required RuleMetadata Metadata { get; init; }
}
