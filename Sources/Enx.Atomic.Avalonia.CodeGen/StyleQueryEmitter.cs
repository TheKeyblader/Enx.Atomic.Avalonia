using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>Emits a <see cref="StyleQueryExpression"/> chain as C# text, walking the data tree directly — see <see cref="SelectorEmitter"/>.</summary>
public static class StyleQueryEmitter
{
    public static string Emit(StyleQueryExpression expression, ISet<string> namespaces)
    {
        namespaces.Add("Avalonia.Styling");

        // Or/And are plain static factory calls in Avalonia (StyleQueries.Or(params StyleQuery[])) — unlike
        // Width/Height/etc., they don't chain fluently off a previous query, so "Previous" is never read here.
        if (expression is StyleQueryExpression.Or or_)
            return $"StyleQueries.Or([{EmitList(or_.Queries, namespaces)}])";
        if (expression is StyleQueryExpression.And and_)
            return $"StyleQueries.And([{EmitList(and_.Queries, namespaces)}])";

        var previous = expression.Previous is null ? "query" : Emit(expression.Previous, namespaces);

        return expression switch
        {
            StyleQueryExpression.Width width =>
                $"{previous}.Width(StyleQueryComparisonOperator.{width.Operator}, {CSharpLiteral.Double(width.Value)})",
            StyleQueryExpression.Height height =>
                $"{previous}.Height(StyleQueryComparisonOperator.{height.Operator}, {CSharpLiteral.Double(height.Value)})",
            _ => throw new NotSupportedException($"Unknown style query node '{expression.GetType()}'."),
        };
    }

    private static string EmitList(StyleQueryExpression[] queries, ISet<string> namespaces) =>
        string.Join(", ", queries.Select(q => Emit(q, namespaces)));
}
