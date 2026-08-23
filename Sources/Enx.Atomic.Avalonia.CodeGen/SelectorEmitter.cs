using Enx.Atomic.Avalonia.CodeGen.Values;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// Emits a <see cref="SelectorExpression"/> chain as C# text (e.g. <c>"selector.Is&lt;Button&gt;().Class(\"hover:bg-red-500\")"</c>),
/// walking the data tree directly rather than compiling it and decompiling the result — see the "Source
/// transformers and ghost properties"/"Build-time C# code generation" sections of <c>ARCHITECTURE.md</c> for
/// why that distinction matters.
/// </summary>
public static class SelectorEmitter
{
    public static string Emit(SelectorExpression expression, ISet<string> namespaces, ValueEmitterRegistry values)
    {
        var previous = expression.Previous is null ? "selector" : Emit(expression.Previous, namespaces, values);

        return expression switch
        {
            SelectorExpression.Is is_ => EmitIs(is_, previous, namespaces),
            SelectorExpression.OfType ofType => EmitOfType(ofType, previous, namespaces),
            SelectorExpression.Class @class => EmitClass(@class, previous),
            SelectorExpression.PropertyEquals propertyEquals => EmitPropertyEquals(
                propertyEquals,
                previous,
                namespaces,
                values
            ),
            _ => throw new NotSupportedException($"Unknown selector node '{expression.GetType()}'."),
        };
    }

    private static string EmitIs(SelectorExpression.Is is_, string previous, ISet<string> namespaces)
    {
        foreach (var ns in CSharpTypeNaming.GetNamespaces(is_.TargetType))
            namespaces.Add(ns);
        return $"{previous}.Is<{CSharpTypeNaming.GetName(is_.TargetType)}>()";
    }

    private static string EmitOfType(SelectorExpression.OfType ofType, string previous, ISet<string> namespaces)
    {
        foreach (var ns in CSharpTypeNaming.GetNamespaces(ofType.TargetType))
            namespaces.Add(ns);
        return $"{previous}.OfType<{CSharpTypeNaming.GetName(ofType.TargetType)}>()";
    }

    private static string EmitClass(SelectorExpression.Class @class, string previous) =>
        $"{previous}.Class({(@class.Name is null ? "null" : CSharpLiteral.String(@class.Name))})";

    private static string EmitPropertyEquals(
        SelectorExpression.PropertyEquals propertyEquals,
        string previous,
        ISet<string> namespaces,
        ValueEmitterRegistry values
    )
    {
        foreach (var ns in CSharpTypeNaming.GetNamespaces(propertyEquals.Property.OwnerType))
            namespaces.Add(ns);

        var propertyText = AvaloniaPropertyNaming.GetExpression(propertyEquals.Property);
        var valueText = values.Emit(propertyEquals.Value, namespaces);
        return $"{previous}.PropertyEquals({propertyText}, {valueText})";
    }
}
