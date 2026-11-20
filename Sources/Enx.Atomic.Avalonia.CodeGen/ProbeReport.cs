using System.Text.Json.Serialization;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// The machine-readable document <c>probe --json</c> prints. Field names are pinned with
/// <see cref="JsonPropertyNameAttribute"/>: agents parse this shape, so renaming a C# property must not
/// silently change the wire contract. Positions are 1-based within the whole probed set, matching the
/// human-readable output.
/// </summary>
internal sealed record ProbeReport
{
    /// <summary>One entry per distinct extracted token, in extraction order.</summary>
    [JsonPropertyName("tokens")]
    public required IReadOnlyList<ProbeTokenReport> Tokens { get; init; }

    /// <summary>Every resource key referenced by a resolved setter, sorted ordinally.</summary>
    [JsonPropertyName("resources")]
    public required IReadOnlyList<ProbeResourceReport> Resources { get; init; }

    /// <summary>Present only when <c>--control</c> was given.</summary>
    [JsonPropertyName("control")]
    public ProbeControlReport? Control { get; init; }
}

/// <summary>How one raw token matched: the variants peeled off, the remaining base token, and the styles it produced.</summary>
internal sealed record ProbeTokenReport
{
    [JsonPropertyName("raw")]
    public required string Raw { get; init; }

    /// <summary>Matched variants, outermost first (the order the token is written in, e.g. <c>dark:hover:</c>).</summary>
    [JsonPropertyName("variants")]
    public required IReadOnlyList<ProbeVariantReport> Variants { get; init; }

    /// <summary>The token with every matched variant stripped — what the rules were matched against.</summary>
    [JsonPropertyName("baseToken")]
    public required string BaseToken { get; init; }

    /// <summary>
    /// The leaf utility tokens a shortcut base token expanded to (nested shortcuts already flattened), or
    /// <see langword="null"/> when the base token isn't a shortcut.
    /// </summary>
    [JsonPropertyName("shortcut")]
    public IReadOnlyList<string>? Shortcut { get; init; }

    [JsonPropertyName("matched")]
    public required bool Matched { get; init; }

    /// <summary>Simple name of the originating rule (e.g. <c>MarginRule</c>), or <see langword="null"/> when nothing matched.</summary>
    [JsonPropertyName("rule")]
    public string? Rule { get; init; }

    /// <summary>The rule's declaration index — the third component of the emission-order key.</summary>
    [JsonPropertyName("ruleIndex")]
    public int? RuleIndex { get; init; }

    /// <summary>The rule's own order rank for this match — the fourth component of the emission-order key.</summary>
    [JsonPropertyName("rank")]
    public int? Rank { get; init; }

    /// <summary>Cheap diagnostics for an unmatched token, e.g. "a variant prefix matched but no rule did".</summary>
    [JsonPropertyName("hints")]
    public required IReadOnlyList<string> Hints { get; init; }

    [JsonPropertyName("styles")]
    public required IReadOnlyList<ProbeStyleReport> Styles { get; init; }
}

/// <summary>One variant peeled off a token, expressed as the text it consumed plus the classes that recognised it.</summary>
internal sealed record ProbeVariantReport
{
    /// <summary>The consumed prefix (<c>hover:</c>, <c>max-sm:</c>) or suffix, when it can be reconstructed from the handler's stripped token.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>Configured variant type name without generic arity (e.g. <c>PseudoClassVariant</c>).</summary>
    [JsonPropertyName("variant")]
    public required string Variant { get; init; }

    /// <summary>The handler record type (e.g. <c>PseudoClassVariantHandler</c>), which carries the concrete values.</summary>
    [JsonPropertyName("handler")]
    public required string Handler { get; init; }
}

/// <summary>One resolved <see cref="StringifiedUtil"/> produced by a token, placed in the final emission order.</summary>
internal sealed record ProbeStyleReport
{
    /// <summary>1-based position in <see cref="StyleOrdering.Sort"/>'s output for the whole probed set.</summary>
    [JsonPropertyName("position")]
    public required int Position { get; init; }

    /// <summary>Total number of styles in the probed set, so an agent can interpret <see cref="Position"/>.</summary>
    [JsonPropertyName("positionCount")]
    public required int PositionCount { get; init; }

    /// <summary>The selector rendered from <see cref="StringifiedUtil.SelectorData"/> (never decompiled).</summary>
    [JsonPropertyName("selector")]
    public required string Selector { get; init; }

    /// <summary>The container query rendered from <see cref="StringifiedUtil.ContainerQueryData"/>, or <see langword="null"/>.</summary>
    [JsonPropertyName("containerQuery")]
    public string? ContainerQuery { get; init; }

    [JsonPropertyName("sortKey")]
    public required ProbeSortKeyReport SortKey { get; init; }

    [JsonPropertyName("setters")]
    public required IReadOnlyList<ProbeSetterReport> Setters { get; init; }
}

/// <summary>The emission-order key's parts, in the order <see cref="StyleOrdering.Sort"/> compares them.</summary>
internal sealed record ProbeSortKeyReport
{
    [JsonPropertyName("classCount")]
    public required int ClassCount { get; init; }

    [JsonPropertyName("typeCount")]
    public required int TypeCount { get; init; }

    /// <summary>The cascade layer: 0 = shortcut, 1 = plain utility — see <see cref="StyleLayer"/>.</summary>
    [JsonPropertyName("layer")]
    public required int Layer { get; init; }

    /// <summary>0 = unconditional, 1 = <c>max-*</c>, 2 = lower-bound breakpoint, 3 = unrecognized.</summary>
    [JsonPropertyName("containerGroup")]
    public required int ContainerGroup { get; init; }

    [JsonPropertyName("containerOrder")]
    public required double ContainerOrder { get; init; }

    [JsonPropertyName("ruleIndex")]
    public required int RuleIndex { get; init; }

    [JsonPropertyName("rank")]
    public required int Rank { get; init; }

    [JsonPropertyName("sort")]
    public required int Sort { get; init; }

    /// <summary>The final tie-breaker: a culture-independent description of the selector chain.</summary>
    [JsonPropertyName("selectorText")]
    public required string SelectorText { get; init; }
}

/// <summary>One <c>Setter</c> from a resolved style.</summary>
internal sealed record ProbeSetterReport
{
    /// <summary>The declaring property expression, e.g. <c>MarginSides.MarginLeftProperty</c>.</summary>
    [JsonPropertyName("property")]
    public required string Property { get; init; }

    /// <summary>The assigned value, formatted invariantly.</summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    /// <summary>The value's CLR type, or <see langword="null"/> for a null value.</summary>
    [JsonPropertyName("valueType")]
    public string? ValueType { get; init; }

    /// <summary>The resource key for a <see cref="StyleValue.Resource"/>-backed setter, or <see langword="null"/>.</summary>
    [JsonPropertyName("resourceKey")]
    public string? ResourceKey { get; init; }
}

/// <summary>A resource key a probed style's <c>DynamicResource</c> lookup depends on.</summary>
internal sealed record ProbeResourceReport
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    /// <summary>
    /// Whether the theme entry splits into Light/Dark dictionaries. <see langword="null"/> when no control was
    /// requested, since deciding it means invoking the resource's <c>ThemeAccess</c> against the theme.
    /// </summary>
    [JsonPropertyName("themed")]
    public bool? Themed { get; init; }
}

/// <summary>The result of applying the probed styles to a real control in a shown headless window.</summary>
internal sealed record ProbeControlReport
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("width")]
    public required double Width { get; init; }

    [JsonPropertyName("pseudoClasses")]
    public required IReadOnlyList<string> PseudoClasses { get; init; }

    /// <summary>How many entries the resource dictionary merged into the window has.</summary>
    [JsonPropertyName("resourceCount")]
    public required int ResourceCount { get; init; }

    /// <summary>The effective value of every property the probed styles set, plus any recomposed composite property.</summary>
    [JsonPropertyName("effectiveValues")]
    public required IReadOnlyList<ProbeEffectiveValueReport> EffectiveValues { get; init; }
}

/// <summary>The value Avalonia actually ended up with for one touched property.</summary>
internal sealed record ProbeEffectiveValueReport
{
    /// <summary>The declaring property expression, e.g. <c>Layoutable.MarginProperty</c>.</summary>
    [JsonPropertyName("property")]
    public required string Property { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("valueType")]
    public string? ValueType { get; init; }

    /// <summary>Set when the property isn't registered on the instantiated control, so nothing was read.</summary>
    [JsonPropertyName("registered")]
    public required bool Registered { get; init; }

    /// <summary>For a recomposed composite property, the side properties that fed it.</summary>
    [JsonPropertyName("composedFrom")]
    public IReadOnlyList<string>? ComposedFrom { get; init; }

    /// <summary>The <c>DynamicResource</c> key that supplied the value, when the style used one.</summary>
    [JsonPropertyName("resourceKey")]
    public string? ResourceKey { get; init; }
}
