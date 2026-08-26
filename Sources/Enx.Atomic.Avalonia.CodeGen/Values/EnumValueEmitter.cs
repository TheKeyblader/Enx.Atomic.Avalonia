namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>Emits any enum value as <c>EnumType.Member</c>, generically — covers every enum Preset.Mini's rules produce (<c>Orientation</c>, <c>TextAlignment</c>, <c>FontWeight</c>, ...) without listing them one by one.</summary>
public sealed class EnumValueEmitter : IValueEmitter
{
    /// <inheritdoc/>
    public bool CanHandle(Type type) => type.IsEnum;

    /// <inheritdoc/>
    public IEnumerable<string> GetNamespaces(object value) => CSharpTypeNaming.GetNamespaces(value.GetType());

    /// <inheritdoc/>
    public string Emit(object value) => $"{CSharpTypeNaming.GetName(value.GetType())}.{value}";
}
