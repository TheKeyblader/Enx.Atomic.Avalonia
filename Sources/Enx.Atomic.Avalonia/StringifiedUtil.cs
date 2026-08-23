using Avalonia.Styling;
using System.Linq.Expressions;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// A fully resolved Avalonia style produced from a utility token: a selector expression, the property
/// setters it applies, and an optional container query. Ready to be applied at runtime via the compiled
/// <see cref="Selector"/>/<see cref="ContainerQuery"/> expressions, or emitted as C# source by walking the
/// uncompiled <see cref="SelectorData"/>/<see cref="ContainerQueryData"/> data trees instead.
/// </summary>
public record StringifiedUtil
{
    /// <summary>The declaration order of the originating rule, used to keep emitted styles deterministically ordered.</summary>
    public required int Index { get; init; }

    /// <summary>Expression building the Avalonia <see cref="Selector"/> this style applies to.</summary>
    public required Expression<Func<Selector, Selector>> Selector { get; set; }

    /// <summary>Expression building the container query to wrap this style in, or <see langword="null"/> if the style is not container-scoped.</summary>
    public Expression<Func<StyleQuery, StyleQuery>>? ContainerQuery { get; set; }

    /// <summary>
    /// The same selector as <see cref="Selector"/>, as the uncompiled data tree it was built from — for
    /// consumers (e.g. a C# source emitter) that need to walk its structure rather than compile/invoke it.
    /// </summary>
    public required SelectorExpression SelectorData { get; set; }

    /// <summary>The same container query as <see cref="ContainerQuery"/>, as the uncompiled data tree, or <see langword="null"/> if none applies.</summary>
    public StyleQueryExpression? ContainerQueryData { get; set; }

    /// <summary>The property setters applied by this style.</summary>
    public required Setter[] Body { get; init; }

    /// <summary>Metadata about the rule that produced this style.</summary>
    public required RuleMetadata Metadata { get; init; }
}
