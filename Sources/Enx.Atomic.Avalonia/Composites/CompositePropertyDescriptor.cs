using Avalonia;

namespace Enx.Atomic.Avalonia.Composites;

/// <summary>
/// Describes one composite Avalonia property (<c>Margin</c>, <c>Padding</c>, <c>BorderThickness</c>,
/// <c>CornerRadius</c>, ...) as four independent <see cref="double"/> sides, plus the static class — the
/// <em>host</em> — that carries those sides as attached properties and recomposes the real value.
///
/// <para>
/// This type is a genuine extension point, not internal plumbing: it is what turns the roadmap's
/// "user-extensible ghost-property registry" into a real feature. The shipped hosts
/// (<see cref="MarginSides"/>, <see cref="PaddingSides"/>, <see cref="BorderThicknessSides"/>,
/// <see cref="CornerRadiusSides"/>) are just four instances of the same pattern — declaring a new composite
/// is a matter of writing an equivalent host class:
/// </para>
/// <list type="number">
///   <item>declaring four public <c>AttachedProperty&lt;double&gt;</c> fields, in the kind's slot order, whose
///   registered names are unique (the shipped ones prefix them with <c>Enx</c>);</item>
///   <item>describing them with <see cref="Create"/> (or this constructor, for non-standard field names);</item>
///   <item>registering a class handler that recomposes the real property from the four sides — the shipped
///   hosts do this through the engine's shared runtime implementation, so a new host only needs wiring;</item>
///   <item>marking the class <see cref="EmittableGhostPropertyHostAttribute"/> so the build-time code
///   generator embeds an equivalent, self-contained copy of it into the generated file (the consuming
///   project's references may not include the host's assembly at all).</item>
/// </list>
///
/// <para>
/// A side is <see cref="double.NaN"/> when it is unset. Unset sides compose as <c>0</c>; when all four are
/// unset the host withdraws its contribution entirely, so the real property falls back to its theme/default
/// value instead of being pinned to a zeroed struct.
/// </para>
/// </summary>
public sealed class CompositePropertyDescriptor
{
    /// <summary>Creates a descriptor from explicit names — the escape hatch for a host whose field or registered names don't follow <see cref="Create"/>'s convention.</summary>
    /// <param name="realProperty">The composite property being decomposed (e.g. <c>Layoutable.MarginProperty</c>).</param>
    /// <param name="kind">The composite's shape, which fixes the slot order — see <see cref="CompositeKind"/>.</param>
    /// <param name="hostTypeName">The simple name of the static host class, which is also the name of the class the code generator embeds.</param>
    /// <param name="sideFields">The host's four public static field names, in the kind's slot order.</param>
    /// <param name="attachedPropertyNames">The four registered attached-property names, in the same slot order.</param>
    /// <exception cref="ArgumentException">A collection doesn't hold exactly four entries, or the host name is empty.</exception>
    public CompositePropertyDescriptor(
        AvaloniaProperty realProperty,
        CompositeKind kind,
        string hostTypeName,
        IReadOnlyList<string> sideFields,
        IReadOnlyList<string> attachedPropertyNames
    )
    {
        ArgumentNullException.ThrowIfNull(realProperty);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostTypeName);
        ArgumentNullException.ThrowIfNull(sideFields);
        ArgumentNullException.ThrowIfNull(attachedPropertyNames);

        if (sideFields.Count != 4 || attachedPropertyNames.Count != 4)
            throw new ArgumentException("A composite property always has exactly four sides.", nameof(sideFields));

        RealProperty = realProperty;
        Kind = kind;
        HostTypeName = hostTypeName;
        SideFields = [.. sideFields];
        AttachedPropertyNames = [.. attachedPropertyNames];
    }

    /// <summary>The real composite property every side contributes to, e.g. <c>Layoutable.MarginProperty</c>.</summary>
    public AvaloniaProperty RealProperty { get; }

    /// <summary>The composite's shape, which defines both the slot order and the struct used to reassemble it.</summary>
    public CompositeKind Kind { get; }

    /// <summary>
    /// The host class's simple name. The code generator embeds its copy under this exact name, and the
    /// registration of each attached property names the host as its owner — so a rule can reach the host's
    /// properties at runtime and the generated file can reach its own copy by the same text.
    /// </summary>
    public string HostTypeName { get; }

    /// <summary>The host's four public static field names, in <see cref="Kind"/>'s slot order.</summary>
    public IReadOnlyList<string> SideFields { get; }

    /// <summary>
    /// The four registered attached-property names, in <see cref="Kind"/>'s slot order. They are prefixed to
    /// stay clear of Avalonia's own property names on the same owner; the emitter reproduces them verbatim.
    /// </summary>
    public IReadOnlyList<string> AttachedPropertyNames { get; }

    /// <summary>
    /// Builds a descriptor for a host following the shipped naming convention: for a <paramref name="sidePrefix"/>
    /// of <c>"Margin"</c>, the fields are <c>MarginLeftProperty</c>, <c>MarginTopProperty</c>, ... and the
    /// registered names <c>EnxMarginLeft</c>, <c>EnxMarginTop</c>, ... (slots named after
    /// <paramref name="kind"/>). Keeping both lists derived from one prefix is what makes the runtime host and
    /// its embedded copy use identical names without a second place to update.
    /// </summary>
    /// <param name="realProperty">The composite property being decomposed.</param>
    /// <param name="kind">The composite's shape — also selects the slot names.</param>
    /// <param name="hostTypeName">The static host class's simple name.</param>
    /// <param name="sidePrefix">The shared prefix of the four side names, e.g. <c>"Margin"</c>.</param>
    public static CompositePropertyDescriptor Create(
        AvaloniaProperty realProperty,
        CompositeKind kind,
        string hostTypeName,
        string sidePrefix
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sidePrefix);
        var slots = SlotNames(kind);
        return new CompositePropertyDescriptor(
            realProperty,
            kind,
            hostTypeName,
            [.. slots.Select(slot => $"{sidePrefix}{slot}Property")],
            [.. slots.Select(slot => $"Enx{sidePrefix}{slot}")]
        );
    }

    /// <summary>The four slot names of <paramref name="kind"/>, in the order the corresponding struct constructor expects them.</summary>
    private static string[] SlotNames(CompositeKind kind) =>
        kind switch
        {
            CompositeKind.Thickness => ["Left", "Top", "Right", "Bottom"],
            CompositeKind.CornerRadius => ["TopLeft", "TopRight", "BottomRight", "BottomLeft"],
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown composite kind."),
        };
}
