using Avalonia.Media;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>
/// Emits a <see cref="FontFamily"/> as <c>new FontFamily("Georgia, Cambria")</c> — the same comma-separated fallback
/// list its constructor parses, so a multi-family value round-trips exactly. <see cref="FontFamily.Default"/> is
/// emitted by name (<c>FontFamily.Default</c>, equal to <c>new FontFamily("$Default")</c>) to keep the output
/// readable.
///
/// <para>
/// A family loaded from a font asset (<c>avares://...#Name</c>) exposes its source only through
/// <see cref="FontFamily.Key"/>, and rebuilding it would need the source URI's scheme — not the comma-separated
/// names, which is all <see cref="FontFamily.FamilyNames"/> carries. That shape can't be reconstructed from public
/// state (no rule in this preset produces one), so it throws instead of emitting something subtly different.
/// </para>
/// </summary>
public sealed class FontFamilyValueEmitter : IValueEmitter
{
    /// <inheritdoc/>
    public bool CanHandle(Type type) => type == typeof(FontFamily);

    /// <inheritdoc/>
    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia.Media"];

    /// <inheritdoc/>
    public string Emit(object value)
    {
        var family = (FontFamily)value;

        if (family.FamilyNames.Count == 1 && family.FamilyNames.PrimaryFamilyName == FontFamily.DefaultFontFamilyName)
            return "FontFamily.Default";

        if (
            family.Key is { } key
            && key.Source.Scheme is not (FontManager.SystemFontScheme or FontManager.CompositeFontScheme)
        )
        {
            throw new NotSupportedException(
                $"Cannot emit FontFamily '{family}' — only families built from a system font name are supported, "
                    + $"not one loaded from '{key.Source}'."
            );
        }

        return $"new FontFamily({CSharpLiteral.String(family.FamilyNames.ToString())})";
    }
}
