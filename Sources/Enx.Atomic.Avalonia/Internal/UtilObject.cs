using Avalonia.Styling;
using System.Linq.Expressions;

namespace Enx.Atomic.Avalonia.Internal;

/// <summary>
/// The output of applying a token's variant handler pipeline: compiled selector/container-query expressions
/// ready to be converted into a <see cref="StringifiedUtil"/>.
/// </summary>
internal record UtilObject
{
    /// <summary>Expression building the final Avalonia selector.</summary>
    public required Expression<Func<Selector, Selector>> Selector { get; set; }

    /// <summary>Expression building the container query, or <see langword="null"/> if none applies.</summary>
    public Expression<Func<StyleQuery, StyleQuery>>? ContainerQuery { get; set; }

    /// <summary>The style values to emit as setters.</summary>
    public StyleValue[] Entries { get; init; } = [];

    /// <summary>Relative ordering hint for the resulting style.</summary>
    public int Sort { get; init; }
}
