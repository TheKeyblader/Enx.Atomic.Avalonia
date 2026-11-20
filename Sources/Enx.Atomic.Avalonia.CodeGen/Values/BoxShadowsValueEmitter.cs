using Avalonia.Media;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>
/// Emits a <see cref="BoxShadows"/> as explicit constructor calls over <see cref="BoxShadow"/> object
/// initializers — never <c>BoxShadows.Parse("...")</c>, whose input would be a culture-sensitive,
/// hand-formatted string that the emitter has to reconstruct correctly, and whose alpha notation is a second
/// parser to keep in sync with Avalonia's. Reconstructs every public component (offsets, blur, spread, colour,
/// inset) so the emitted value is byte-for-byte the runtime one.
/// <para>
/// The no-shadow case (<see cref="BoxShadows.Count"/> == 0, what <c>shadow-none</c> resolves to) is emitted as
/// <c>new BoxShadows()</c>, the same default value <see cref="BoxShadows.Parse"/> returns for <c>"none"</c>.
/// </para>
/// </summary>
public sealed class BoxShadowsValueEmitter : IValueEmitter
{
    /// <inheritdoc/>
    public bool CanHandle(Type type) => type == typeof(BoxShadows);

    /// <inheritdoc/>
    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia.Media"];

    /// <inheritdoc/>
    public string Emit(object value)
    {
        var shadows = (BoxShadows)value;
        if (shadows.Count == 0)
            return "new BoxShadows()";

        if (shadows.Count == 1)
            return $"new BoxShadows({EmitLayer(shadows[0])})";

        var rest = string.Join(", ", Enumerable.Range(1, shadows.Count - 1).Select(i => EmitLayer(shadows[i])));
        return $"new BoxShadows({EmitLayer(shadows[0])}, new BoxShadow[] {{ {rest} }})";
    }

    /// <summary>One layer, with every property named explicitly so the output doesn't depend on positional ctor overloads (there are none) or default ordering.</summary>
    private static string EmitLayer(BoxShadow shadow) =>
        $"new BoxShadow {{ OffsetX = {CSharpLiteral.Double(shadow.OffsetX)}, OffsetY = {CSharpLiteral.Double(shadow.OffsetY)}, "
        + $"Blur = {CSharpLiteral.Double(shadow.Blur)}, Spread = {CSharpLiteral.Double(shadow.Spread)}, "
        + $"Color = Color.FromArgb({shadow.Color.A}, {shadow.Color.R}, {shadow.Color.G}, {shadow.Color.B}), "
        + $"IsInset = {(shadow.IsInset ? "true" : "false")} }}";
}
