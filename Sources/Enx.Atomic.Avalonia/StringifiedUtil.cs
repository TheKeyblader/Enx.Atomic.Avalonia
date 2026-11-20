using System.Linq.Expressions;
using Avalonia.Styling;
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

    /// <summary>
    /// Relative order of this util <em>within</em> its originating rule — the value an
    /// <see cref="IOrderRankedDynamicRule{TTheme}"/> reported for its match, so a rule that consolidates what
    /// UnoCSS keeps as several entries (<c>m-*</c> &lt; <c>mx-*</c> &lt; <c>ml-*</c>) still emits in the order
    /// those entries would have. Sorted right after <see cref="Index"/> and before <see cref="Sort"/>; always
    /// <c>0</c> for a rule that doesn't implement that interface.
    /// </summary>
    public int Rank { get; init; }

    /// <summary>
    /// Relative ordering hint a variant handler may set (propagated from <c>VariantHandlerContext.Sort</c>), used by
    /// <see cref="StyleOrdering"/> as a tie-breaker after the rule index and rank — the equivalent of UnoCSS's per-rule
    /// <c>sort</c> context. No variant in the Mini preset sets it today; it stays <c>0</c> for every rule.
    /// </summary>
    public int Sort { get; init; }

    /// <summary>
    /// The cascade layer this style belongs to, compared by <see cref="StyleOrdering"/> after selector specificity
    /// and before any container query — see <see cref="StyleLayer"/>. A style expanded from a shortcut is
    /// <see cref="StyleLayer.Shortcuts"/> (<c>0</c>), a plain utility is <see cref="StyleLayer.Default"/> (<c>1</c>,
    /// the default so a util built outside token resolution sorts like a utility). Like UnoCSS emitting its
    /// <c>shortcuts</c> layer before <c>default</c>, this makes <c>class="btn bg-red-500"</c> red even when
    /// <c>btn</c> contains a <c>sm:</c>/<c>dark:</c> variant, while a more specific selector such as
    /// <c>hover:btn</c> still outranks a plain utility because specificity is compared first.
    /// </summary>
    public int Layer { get; init; } = StyleLayer.Default;

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
