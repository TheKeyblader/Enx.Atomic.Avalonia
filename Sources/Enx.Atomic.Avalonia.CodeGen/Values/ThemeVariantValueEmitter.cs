using System.Reflection;
using Avalonia.Styling;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>
/// Emits an Avalonia <see cref="ThemeVariant"/> singleton (<see cref="ThemeVariant.Light"/>, <see cref="ThemeVariant.Dark"/>, ...)
/// as <c>ThemeVariant.Member</c>, found via its declaring public static property — the same technique
/// <see cref="AvaloniaPropertyNaming"/> uses for <c>AvaloniaProperty</c> fields, but over properties, since
/// <see cref="ThemeVariant"/>'s singletons are exposed as properties rather than fields.
/// </summary>
public sealed class ThemeVariantValueEmitter : IValueEmitter
{
    public bool CanHandle(Type type) => type == typeof(ThemeVariant);

    public IEnumerable<string> GetNamespaces(object value) => CSharpTypeNaming.GetNamespaces(typeof(ThemeVariant));

    public string Emit(object value)
    {
        foreach (var prop in typeof(ThemeVariant).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (prop.PropertyType != typeof(ThemeVariant) || !Equals(prop.GetValue(null), value))
                continue;

            return $"{CSharpTypeNaming.GetName(typeof(ThemeVariant))}.{prop.Name}";
        }

        throw new NotSupportedException($"No public static ThemeVariant property holds the value '{value}'.");
    }
}
