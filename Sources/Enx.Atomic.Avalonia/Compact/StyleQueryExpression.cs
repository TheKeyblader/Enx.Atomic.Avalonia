using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Styling;

namespace Enx.Atomic.Avalonia.Compact;

/// <summary>
/// A serializable, linked-list representation of an Avalonia <see cref="StyleQuery"/> chain (a container
/// query, e.g. <c>Width(GreaterThan, 600)</c>), mirroring <see cref="SelectorExpression"/>: built up as data
/// by variant handlers and converted to a compiled <see cref="Expression"/> via <see cref="ToExpression(Expression)"/>.
/// </summary>
public abstract record StyleQueryExpression
{
    /// <summary>The previous link in the chain, applied before this one, or <see langword="null"/> if this is the first.</summary>
    public StyleQueryExpression? Previous { get; set; }

    /// <summary>Builds this node's contribution to the expression tree, given the already-built <paramref name="parameter"/> expression.</summary>
    public abstract Expression ToExpressionCore(Expression parameter);

    /// <summary>Builds the full expression tree for this chain, applying <see cref="Previous"/> first if present.</summary>
    public Expression ToExpression(Expression parameter)
    {
        if (Previous is not null)
            parameter = Previous.ToExpression(parameter);
        return ToExpressionCore(parameter);
    }

    /// <summary>Compares the container's width, via <see cref="StyleQueries.Width"/>.</summary>
    public record Width : StyleQueryExpression
    {
        private static readonly MethodInfo CallInfo;

        static Width()
        {
            CallInfo =
                typeof(StyleQueries).GetMethod(
                    nameof(StyleQueries.Width),
                    BindingFlags.Static | BindingFlags.Public,
                    [typeof(StyleQuery), typeof(StyleQueryComparisonOperator), typeof(double)]
                ) ?? throw new InvalidOperationException();
        }

        /// <summary>The comparison to apply.</summary>
        public StyleQueryComparisonOperator Operator { get; set; }

        /// <summary>The width to compare against.</summary>
        public double Value { get; set; }

        /// <summary>Creates a width-comparison query node.</summary>
        public Width(
            StyleQueryExpression? previous,
            StyleQueryComparisonOperator @operator,
            double value
        )
        {
            Previous = previous;
            Operator = @operator;
            Value = value;
        }

        /// <inheritdoc/>
        public override Expression ToExpressionCore(Expression parameter)
        {
            return Expression.Call(
                CallInfo,
                [parameter, Expression.Constant(Operator), Expression.Constant(Value)]
            );
        }
    }

    /// <summary>Compares the container's height, via <see cref="StyleQueries.Height"/>.</summary>
    public record Height : StyleQueryExpression
    {
        private static readonly MethodInfo CallInfo;

        static Height()
        {
            CallInfo =
                typeof(StyleQueries).GetMethod(
                    nameof(StyleQueries.Height),
                    BindingFlags.Static | BindingFlags.Public,
                    [typeof(StyleQuery), typeof(StyleQueryComparisonOperator), typeof(double)]
                ) ?? throw new InvalidOperationException();
        }

        /// <summary>The comparison to apply.</summary>
        public StyleQueryComparisonOperator Operator { get; set; }

        /// <summary>The height to compare against.</summary>
        public double Value { get; set; }

        /// <summary>Creates a height-comparison query node.</summary>
        public Height(
            StyleQueryExpression? previous,
            StyleQueryComparisonOperator @operator,
            double value
        )
        {
            Previous = previous;
            Operator = @operator;
            Value = value;
        }

        /// <inheritdoc/>
        public override Expression ToExpressionCore(Expression parameter)
        {
            return Expression.Call(
                CallInfo,
                [parameter, Expression.Constant(Operator), Expression.Constant(Value)]
            );
        }
    }

    /// <summary>Combines several queries with OR semantics, via <see cref="StyleQueries.Or"/>.</summary>
    public record Or : StyleQueryExpression
    {
        private static readonly MethodInfo CallInfo;

        static Or()
        {
            CallInfo =
                typeof(StyleQueries).GetMethod(
                    nameof(StyleQueries.Or),
                    BindingFlags.Static | BindingFlags.Public,
                    [typeof(StyleQuery[])]
                ) ?? throw new InvalidOperationException();
        }

        /// <summary>The queries to OR together.</summary>
        public StyleQueryExpression[] Queries { get; set; }

        /// <summary>Creates an OR query node.</summary>
        public Or(StyleQueryExpression? previous, StyleQueryExpression[] queries)
        {
            Previous = previous;
            Queries = queries;
        }

        /// <inheritdoc/>
        public override Expression ToExpressionCore(Expression parameter)
        {
            var queries = Queries.Select(x => x.ToExpression(parameter)).ToArray();

            var arrayExpression = Expression.NewArrayInit(typeof(StyleQuery), queries);
            return Expression.Call(CallInfo, [arrayExpression]);
        }
    }

    /// <summary>Combines several queries with AND semantics, via <see cref="StyleQueries.And"/>.</summary>
    public record And : StyleQueryExpression
    {
        private static readonly MethodInfo CallInfo;

        static And()
        {
            CallInfo =
                typeof(StyleQueries).GetMethod(
                    nameof(StyleQueries.And),
                    BindingFlags.Static | BindingFlags.Public,
                    [typeof(StyleQuery[])]
                ) ?? throw new InvalidOperationException();
        }

        /// <summary>The queries to AND together.</summary>
        public StyleQueryExpression[] Queries { get; set; }

        /// <summary>Creates an AND query node.</summary>
        public And(StyleQueryExpression? previous, StyleQueryExpression[] queries)
        {
            Previous = previous;
            Queries = queries;
        }

        /// <inheritdoc/>
        public override Expression ToExpressionCore(Expression parameter)
        {
            var queries = Queries.Select(x => x.ToExpression(parameter)).ToArray();

            var arrayExpression = Expression.NewArrayInit(typeof(StyleQuery), queries);
            return Expression.Call(CallInfo, [arrayExpression]);
        }
    }
}

/// <summary>Fluent factory methods for chaining <see cref="StyleQueryExpression"/> nodes.</summary>
public static class StyleQueriesExtensions
{
    /// <summary>Appends a <see cref="StyleQueryExpression.Height"/> node comparing the container's height.</summary>
    public static StyleQueryExpression Height(
        this StyleQueryExpression? previous,
        StyleQueryComparisonOperator @operator,
        double value
    ) => new StyleQueryExpression.Height(previous, @operator, value);

    /// <summary>Appends a <see cref="StyleQueryExpression.Width"/> node comparing the container's width.</summary>
    public static StyleQueryExpression Width(
        this StyleQueryExpression? previous,
        StyleQueryComparisonOperator @operator,
        double value
    ) => new StyleQueryExpression.Width(previous, @operator, value);

    /// <summary>Appends a <see cref="StyleQueryExpression.Or"/> node combining <paramref name="queries"/>.</summary>
    public static StyleQueryExpression Or(
        this StyleQueryExpression? previous,
        StyleQueryExpression[] queries
    ) => new StyleQueryExpression.Or(previous, queries);

    /// <summary>Appends a <see cref="StyleQueryExpression.And"/> node combining <paramref name="queries"/>.</summary>
    public static StyleQueryExpression And(
        this StyleQueryExpression? previous,
        StyleQueryExpression[] queries
    ) => new StyleQueryExpression.And(previous, queries);
}
