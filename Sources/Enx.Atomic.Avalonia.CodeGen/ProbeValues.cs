using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Enx.Atomic.Avalonia.CodeGen.Values;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// Invariant, human-readable formatting shared by the probe's human and JSON renderers, plus the two
/// "render the data tree without compiling it" helpers — the same rule the emitters follow (see
/// <c>ARCHITECTURE.md</c>, "Build-time C# code generation").
/// </summary>
internal static class ProbeValues
{
    /// <summary>The selector chain as C# text built from <see cref="SelectorExpression"/> data, never by decompiling an expression.</summary>
    public static string Selector(SelectorExpression? selector) =>
        selector is null
            ? "(none)"
            : SelectorEmitter.Emit(selector, new HashSet<string>(StringComparer.Ordinal), new ValueEmitterRegistry());

    /// <summary>The container query as C# text built from <see cref="StyleQueryExpression"/> data.</summary>
    public static string ContainerQuery(StyleQueryExpression? query) =>
        query is null ? "(none)" : StyleQueryEmitter.Emit(query, new HashSet<string>(StringComparer.Ordinal));

    /// <summary>The declaring static field expression for a property, e.g. <c>MarginSides.MarginLeftProperty</c>.</summary>
    public static string Property(AvaloniaProperty property)
    {
        try
        {
            return AvaloniaPropertyNaming.GetExpression(property);
        }
        catch (InvalidOperationException)
        {
            return $"{property.OwnerType.Name}.{property.Name}Property";
        }
    }

    /// <summary>A setter/effective value formatted invariantly — boxed numbers must not depend on the process culture.</summary>
    public static string Value(object? value) =>
        value switch
        {
            null => "null",
            DynamicResourceExtension dynamicResource => $"DynamicResource({dynamicResource.ResourceKey})",
            Thickness thickness => string.Create(
                CultureInfo.InvariantCulture,
                $"{thickness.Left},{thickness.Top},{thickness.Right},{thickness.Bottom}"
            ),
            CornerRadius cornerRadius => string.Create(
                CultureInfo.InvariantCulture,
                $"{cornerRadius.TopLeft},{cornerRadius.TopRight},{cornerRadius.BottomRight},{cornerRadius.BottomLeft}"
            ),
            ISolidColorBrush brush => brush.Color.ToString(),
            ColumnDefinitions columns => $"{columns.Count} column definition(s)",
            RowDefinitions rows => $"{rows.Count} row definition(s)",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? value.GetType().Name,
        };

    /// <summary>A type's simple name without generic arity — <c>MarginRule`1</c> reads as <c>MarginRule</c>.</summary>
    public static string TypeName(Type type)
    {
        var name = type.Name;
        var arity = name.IndexOf('`', StringComparison.Ordinal);
        return arity < 0 ? name : name[..arity];
    }

    /// <summary>Formats a version/comparison operator for the human output.</summary>
    public static string Invariant(double value) => value.ToString(CultureInfo.InvariantCulture);
}
