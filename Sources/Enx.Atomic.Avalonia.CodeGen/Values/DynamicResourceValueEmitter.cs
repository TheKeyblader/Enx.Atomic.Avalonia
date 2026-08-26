using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>
/// Emits a <see cref="DynamicResourceExtension"/> value — a <see cref="StyleValue.Resource"/>'s
/// <c>UntypedValue</c> — as <c>new DynamicResourceExtension("key")</c>. Purely a <c>Setter</c>-value emitter:
/// it only needs the resource key text, never the theme value the key actually resolves to at runtime — that's
/// <c>ResourceDictionaryEmitter</c>'s job, built from <see cref="StyleValue.Resource.ThemeAccess"/> instead.
/// </summary>
public sealed class DynamicResourceValueEmitter : IValueEmitter
{
    public bool CanHandle(Type type) => type == typeof(DynamicResourceExtension);

    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia.Markup.Xaml.MarkupExtensions"];

    public string Emit(object value) =>
        $"new DynamicResourceExtension({CSharpLiteral.String(((DynamicResourceExtension)value).ResourceKey?.ToString() ?? string.Empty)})";
}
