using Avalonia.Media;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>
/// Emits Avalonia's <see cref="TextTrimming"/> strategy for <c>truncate</c>/<c>text-clip</c>/<c>text-ellipsis</c>.
/// Unlike an enum, <see cref="TextTrimming"/> is an abstract class whose values are public static properties
/// (<c>TextTrimming.None</c>, <c>TextTrimming.CharacterEllipsis</c>, ...): Preset.Mini only ever assigns one of
/// those singletons, so reference identity is enough to reconstruct a standalone reference. A custom strategy
/// subclass has no public state to round-trip and throws instead of emitting something that wouldn't compile.
/// </summary>
public sealed class TextTrimmingValueEmitter : IValueEmitter
{
    /// <inheritdoc/>
    public bool CanHandle(Type type) => typeof(TextTrimming).IsAssignableFrom(type);

    /// <inheritdoc/>
    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia.Media"];

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">The value isn't one of <see cref="TextTrimming"/>'s named static strategies.</exception>
    public string Emit(object value) =>
        value switch
        {
            _ when ReferenceEquals(value, TextTrimming.None) => "TextTrimming.None",
            _ when ReferenceEquals(value, TextTrimming.CharacterEllipsis) => "TextTrimming.CharacterEllipsis",
            _ when ReferenceEquals(value, TextTrimming.WordEllipsis) => "TextTrimming.WordEllipsis",
            _ when ReferenceEquals(value, TextTrimming.PrefixCharacterEllipsis) =>
                "TextTrimming.PrefixCharacterEllipsis",
            _ when ReferenceEquals(value, TextTrimming.LeadingCharacterEllipsis) =>
                "TextTrimming.LeadingCharacterEllipsis",
            _ when ReferenceEquals(value, TextTrimming.PathSegmentEllipsis) => "TextTrimming.PathSegmentEllipsis",
            _ => throw new NotSupportedException("Cannot emit a custom TextTrimming strategy."),
        };
}
