using Avalonia.Styling;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// The canonical emission order for resolved <see cref="StringifiedUtil"/>s, shared by the runtime
/// <c>AtomicGenerator.Generate</c> path and by <c>AtomicCli</c>'s build-time output so both produce the same cascade.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia has no CSS specificity, and the property value that survives is decided by priority first, frame order
/// second: a style whose selector carries an activator is framed at
/// <see cref="Avalonia.Data.BindingPriority.StyleTrigger"/> (<c>StyleInstance.GetPriority</c>), and a plain class
/// selector <em>is</em> an activator. So <c>.w-4</c> and <c>.hover:w-8:pointerover</c> — or <c>.w-4</c> and a
/// <c>min-width:640</c> container query holding <c>.sm:w-8</c> — always tie, and <c>ValueStore.InsertFrame</c> plus
/// <c>ReevaluateEffectiveValues</c> resolve the tie in favour of the <em>last inserted</em> frame. Emitted in source
/// order, <c>hover:w-8 w-4</c> would therefore silently lose its hover state.
/// </para>
/// <para>
/// Ordering here reproduces the cascade UnoCSS gets for free (specificity, then ascending <c>@media</c> blocks, then
/// rule order), so callers never have to know about frame insertion: later utilities win. The key, ascending:
/// </para>
/// <list type="number">
/// <item><b>Specificity</b> — class-like selector nodes (<c>Class</c>, including Avalonia pseudo-classes, and
/// <c>PropertyEquals</c>, e.g. <c>dark:</c>) as CSS's class/attribute/pseudo-class column, then type nodes
/// (<c>Is</c>/<c>OfType</c>) as its lower type column. <c>.hover:bg-red-500:pointerover</c> (2) beats
/// <c>.sm\:bg-red-500</c> (1) regardless of where its container query falls, exactly as in CSS.</item>
/// <item><b>Layer</b> — a shortcut's styles (<see cref="StyleLayer.Shortcuts"/>) before plain utilities
/// (<see cref="StyleLayer.Default"/>), the equivalent of UnoCSS emitting its <c>shortcuts</c> layer before
/// <c>default</c>. A class count of 2 still wins because specificity is compared first, so <c>hover:btn</c>
/// beats a plain utility while <c>btn</c>'s own <c>sm:</c> variant loses to one.</item>
/// <item><b>Container order</b> — unconditional utilities first, then <c>max-*</c> upper bounds (narrowest last),
/// then the <c>sm:</c>/<c>md:</c>/... lower bounds ascending by breakpoint size, mirroring UnoCSS's
/// <c>parentOrder</c> for breakpoints (wider breakpoints are emitted later, as with ascending media blocks).</item>
/// <item><b>Rule index</b> — the declaration order of the originating rule (see <see cref="StringifiedUtil.Index"/>).</item>
/// <item><b>Rule rank</b> — the originating rule's own rank for this match (see <see cref="StringifiedUtil.Rank"/>),
/// so a rule that consolidates what UnoCSS keeps as several entries still emits them in that order
/// (<c>m-*</c> before <c>mx-*</c> before <c>ml-*</c>).</item>
/// <item><b>Variant sort hint</b> — see <see cref="StringifiedUtil.Sort"/>.</item>
/// <item><b>Selector text</b> — a stable, culture-independent description, so otherwise identical styles still come
/// out in one deterministic order (e.g. the three owner-scoped utilities <c>bg-*</c> produces).</item>
/// </list>
/// </remarks>
public static class StyleOrdering
{
    /// <summary>Returns <paramref name="utils"/> in the canonical emission order documented on <see cref="StyleOrdering"/>.</summary>
    /// <param name="utils">The resolved utilities to order, in any order.</param>
    public static StringifiedUtil[] Sort(IEnumerable<StringifiedUtil> utils) =>
        [
            .. utils
                .Select(util => (Util: util, Key: GetKey(util)))
                .OrderBy(x => x.Key.ClassCount)
                .ThenBy(x => x.Key.TypeCount)
                .ThenBy(x => x.Key.Layer)
                .ThenBy(x => x.Key.ContainerGroup)
                .ThenBy(x => x.Key.ContainerOrder)
                .ThenBy(x => x.Key.RuleIndex)
                .ThenBy(x => x.Key.Rank)
                .ThenBy(x => x.Key.Sort)
                .ThenBy(x => x.Key.Text, StringComparer.Ordinal)
                .Select(x => x.Util),
        ];

    /// <summary>
    /// Returns <paramref name="utils"/> without structurally identical duplicates, keeping the first occurrence
    /// of each — same selector data, same container query, and same setters (property instance, and value for
    /// value types).
    /// </summary>
    /// <remarks>
    /// Token-level deduplication already happens in <c>AtomicGenerator.Generate(ISet&lt;string&gt;, ...)</c>, so
    /// rule output never needs this. It exists for the other half of a multi-source build: an
    /// <c>ISourceTransformer</c> registers styles through <c>AtomicGenerator.AddUtil</c>, which has no token to
    /// key on, and the same transformer output therefore arrives once per file it ran over. Reference-typed
    /// setter values only compare equal when they are the same instance, so two distinct resources are never
    /// merged on a guess.
    /// </remarks>
    /// <param name="utils">The utilities to deduplicate, in any order.</param>
    public static StringifiedUtil[] Distinct(IEnumerable<StringifiedUtil> utils)
    {
        var distinct = new List<StringifiedUtil>();
        foreach (var util in utils)
        {
            if (!distinct.Any(candidate => AreIdentical(candidate, util)))
                distinct.Add(util);
        }

        return [.. distinct];
    }

    /// <summary>Upper-bound container conditions (<c>max-sm:</c>), emitted after unconditional utilities.</summary>
    private const int MaxWidthGroup = 1;

    /// <summary>Lower-bound container conditions (<c>sm:</c>, <c>md:</c>, ...), emitted last by ascending size.</summary>
    private const int MinWidthGroup = 2;

    /// <summary>A container condition <see cref="ClassifyContainerNode"/> doesn't understand; still after unconditional utilities.</summary>
    private const int UnclassifiedContainerGroup = 3;

    /// <summary>
    /// The individual components of the ordering key <see cref="Sort"/> compares, in that same ascending
    /// order. Exposed so diagnostics (the CLI's <c>probe</c> command) can explain <em>why</em> a style landed
    /// where it did without re-deriving the cascade in a second place that could drift from this one.
    /// </summary>
    public readonly record struct SortKey
    {
        /// <summary>Class-like selector nodes (CSS's class/attribute/pseudo-class column).</summary>
        public int ClassCount { get; init; }

        /// <summary>Type selector nodes (<c>Is</c>/<c>OfType</c>, CSS's lower type column).</summary>
        public int TypeCount { get; init; }

        /// <summary>The cascade layer — <see cref="StyleLayer.Shortcuts"/> (0) before <see cref="StyleLayer.Default"/> (1).</summary>
        public int Layer { get; init; }

        /// <summary>Unconditional = 0, upper-bound <c>max-*</c> queries = 1, lower-bound queries = 2, unrecognized = 3.</summary>
        public int ContainerGroup { get; init; }

        /// <summary>Order within <see cref="ContainerGroup"/> (breakpoint size, negated for upper bounds).</summary>
        public double ContainerOrder { get; init; }

        /// <summary>The originating rule's declaration index — see <see cref="StringifiedUtil.Index"/>.</summary>
        public int RuleIndex { get; init; }

        /// <summary>The originating rule's rank for this match — see <see cref="StringifiedUtil.Rank"/>.</summary>
        public int Rank { get; init; }

        /// <summary>The variant sort hint — see <see cref="StringifiedUtil.Sort"/>.</summary>
        public int Sort { get; init; }

        /// <summary>Culture-independent selector description, the final tie-breaker.</summary>
        public string Text { get; init; }
    }

    /// <summary>Returns the ordering key <see cref="Sort"/> uses for <paramref name="util"/> — see <see cref="SortKey"/>.</summary>
    public static SortKey GetSortKey(StringifiedUtil util) => GetKey(util);

    private static SortKey GetKey(StringifiedUtil util)
    {
        var (classCount, typeCount) = GetSpecificity(util.SelectorData);
        var (containerGroup, containerOrder) = GetContainerOrder(util.ContainerQueryData);
        return new SortKey
        {
            ClassCount = classCount,
            TypeCount = typeCount,
            Layer = util.Layer,
            ContainerGroup = containerGroup,
            ContainerOrder = containerOrder,
            RuleIndex = util.Index,
            Rank = util.Rank,
            Sort = util.Sort,
            Text = DescribeSelector(util.SelectorData),
        };
    }

    /// <summary>
    /// Counts the selector's class-like nodes (CSS's "class/attribute/pseudo-class" column) and type nodes (its
    /// lower "type" column). Avalonia pseudo-classes are ordinary <see cref="SelectorExpression.Class"/> nodes whose
    /// name starts with <c>:</c>, so they land in the class column exactly like CSS's <c>:hover</c>.
    /// </summary>
    private static (int ClassCount, int TypeCount) GetSpecificity(SelectorExpression? selector)
    {
        var classCount = 0;
        var typeCount = 0;

        // SelectorExpression is a linked list applied root-first; every node contributes equally to specificity, so
        // walking Previous is enough. It has no combinator/Or node today — if one is ever added, its branches must
        // contribute the maximum of their own counts here rather than a sum.
        for (var node = selector; node is not null; node = node.Previous)
        {
            switch (node)
            {
                case SelectorExpression.Class:
                case SelectorExpression.PropertyEquals:
                    classCount++;
                    break;

                case SelectorExpression.Is:
                case SelectorExpression.OfType:
                    typeCount++;
                    break;
            }
        }

        return (classCount, typeCount);
    }

    /// <summary>
    /// Ranks a style's container condition, following UnoCSS's breakpoint <c>parentOrder</c>: unconditional styles
    /// first, then upper bounds (a larger <c>max-*</c> is less specific, so it sorts earlier), then lower bounds in
    /// ascending size — the same order as ascending CSS media blocks, where the later, wider block wins.
    /// </summary>
    private static (int Group, double Order) GetContainerOrder(StyleQueryExpression? query)
    {
        if (query is null)
            return (0, 0);

        var key = (Group: 0, Order: 0d);
        for (var node = query; node is not null; node = node.Previous)
        {
            var nodeKey = ClassifyContainerNode(node);
            if (nodeKey.CompareTo(key) > 0)
                key = nodeKey;
        }

        // A container condition nothing here recognizes — a node type added after this method was written — must
        // still sort after a plain utility, or a future variant would silently stop overriding its base.
        return key.Group == 0 ? (UnclassifiedContainerGroup, 0) : key;
    }

    private static (int Group, double Order) ClassifyContainerNode(StyleQueryExpression node) =>
        node switch
        {
            // max-*: several upper bounds can match at once (max-md and max-sm both do below 640); the narrowest is
            // the most specific and must be emitted last, hence the negated size so ascending == descending bound.
            StyleQueryExpression.Width
            {
                Operator: StyleQueryComparisonOperator.LessThan or StyleQueryComparisonOperator.LessThanOrEquals,
            } width => (MaxWidthGroup, -width.Value),
            StyleQueryExpression.Height
            {
                Operator: StyleQueryComparisonOperator.LessThan or StyleQueryComparisonOperator.LessThanOrEquals,
            } height => (MaxWidthGroup, -height.Value),

            // sm:/md:/...: only lower bounds tie with each other, and the widest matching one must win (CSS emits
            // ascending media blocks, so md comes after sm).
            StyleQueryExpression.Width width => (MinWidthGroup, width.Value),

            // No Mini variant produces a height query yet; a height condition is an "at least" constraint like a
            // min-width, so it shares that bucket to keep outranking the unconditional base.
            StyleQueryExpression.Height height => (MinWidthGroup, height.Value),

            // Combined conditions: the most restrictive branch decides (max of the branches, not their sum).
            StyleQueryExpression.And andQuery => GetCombinedContainerOrder(andQuery.Queries),
            StyleQueryExpression.Or orQuery => GetCombinedContainerOrder(orQuery.Queries),

            _ => (0, 0),
        };

    private static (int Group, double Order) GetCombinedContainerOrder(StyleQueryExpression[] queries)
    {
        var key = (Group: 0, Order: 0d);
        foreach (var query in queries)
        {
            var queryKey = GetContainerOrder(query);
            if (queryKey.CompareTo(key) > 0)
                key = queryKey;
        }

        return key;
    }

    /// <summary>
    /// Whether two utilities describe exactly the same style. Selector/query data are expression records, so
    /// their generated structural equality walks the whole chain; setter values use <see cref="object.Equals(object)"/>
    /// for the same reason — a boxed <c>double</c> compares by value, a brush or binding by identity. The layer is
    /// part of the identity too: a shortcut's copy of a style is not interchangeable with a plain utility's,
    /// since they sort differently.
    /// </summary>
    private static bool AreIdentical(StringifiedUtil left, StringifiedUtil right) =>
        left.Layer == right.Layer
        && Equals(left.SelectorData, right.SelectorData)
        && Equals(left.ContainerQueryData, right.ContainerQueryData)
        && left.Body.Length == right.Body.Length
        && left.Body.Zip(right.Body)
            .All(pair =>
                ReferenceEquals(pair.First.Property, pair.Second.Property)
                && Equals(pair.First.Value, pair.Second.Value)
            );

    /// <summary>
    /// A deterministic, culture-independent text form of the selector chain, used only as the final tie-breaker so
    /// that utilities with an identical ordering key still emit in a stable order.
    /// </summary>
    private static string DescribeSelector(SelectorExpression? selector)
    {
        var parts = new List<string>();
        for (var node = selector; node is not null; node = node.Previous)
        {
            parts.Add(
                node switch
                {
                    SelectorExpression.Class className => "." + className.Name,
                    SelectorExpression.PropertyEquals propertyEquals =>
                        $"[{propertyEquals.Property.Name}={propertyEquals.Value}]",
                    SelectorExpression.Is isNode => "<" + isNode.TargetType.FullName + ">",
                    SelectorExpression.OfType ofTypeNode => "<<" + ofTypeNode.TargetType.FullName + ">>",
                    _ => node.GetType().Name,
                }
            );
        }

        parts.Reverse();
        return string.Join(' ', parts);
    }
}
