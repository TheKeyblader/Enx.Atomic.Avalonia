using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Styling;

namespace Enx.Atomic.Avalonia.Compact;

public abstract record StyleQueryExpression
{
    public StyleQueryExpression? Previous { get; set; }

    public abstract Expression ToExpressionCore(Expression parameter);

    public Expression ToExpression(Expression parameter)
    {
        if (Previous is not null)
            parameter = Previous.ToExpression(parameter);
        return ToExpressionCore(parameter);
    }

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

        public StyleQueryComparisonOperator Operator { get; set; }
        public double Value { get; set; }

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

        public override Expression ToExpressionCore(Expression parameter)
        {
            return Expression.Call(
                CallInfo,
                [parameter, Expression.Constant(Operator), Expression.Constant(Value)]
            );
        }
    }

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

        public StyleQueryComparisonOperator Operator { get; set; }
        public double Value { get; set; }

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

        public override Expression ToExpressionCore(Expression parameter)
        {
            return Expression.Call(
                CallInfo,
                [parameter, Expression.Constant(Operator), Expression.Constant(Value)]
            );
        }
    }

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

        public StyleQueryExpression[] Queries { get; set; }

        public Or(StyleQueryExpression? previous, StyleQueryExpression[] queries)
        {
            Previous = previous;
            Queries = queries;
        }

        public override Expression ToExpressionCore(Expression parameter)
        {
            var queries = Queries.Select(x => x.ToExpression(parameter)).ToArray();

            var arrayExpression = Expression.NewArrayInit(typeof(StyleQuery), queries);
            return Expression.Call(CallInfo, [arrayExpression]);
        }
    }

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

        public StyleQueryExpression[] Queries { get; set; }

        public And(StyleQueryExpression? previous, StyleQueryExpression[] queries)
        {
            Previous = previous;
            Queries = queries;
        }

        public override Expression ToExpressionCore(Expression parameter)
        {
            var queries = Queries.Select(x => x.ToExpression(parameter)).ToArray();

            var arrayExpression = Expression.NewArrayInit(typeof(StyleQuery), queries);
            return Expression.Call(CallInfo, [arrayExpression]);
        }
    }
}

public static class StyleQueriesExtensions
{
    public static StyleQueryExpression Height(
        this StyleQueryExpression? previous,
        StyleQueryComparisonOperator @operator,
        double value
    ) => new StyleQueryExpression.Height(previous, @operator, value);

    public static StyleQueryExpression Width(
        this StyleQueryExpression? previous,
        StyleQueryComparisonOperator @operator,
        double value
    ) => new StyleQueryExpression.Width(previous, @operator, value);

    public static StyleQueryExpression Or(
        this StyleQueryExpression? previous,
        StyleQueryExpression[] queries
    ) => new StyleQueryExpression.Or(previous, queries);

    public static StyleQueryExpression And(
        this StyleQueryExpression? previous,
        StyleQueryExpression[] queries
    ) => new StyleQueryExpression.And(previous, queries);
}
