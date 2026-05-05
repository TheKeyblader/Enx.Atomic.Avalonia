using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Styling;

namespace Enx.Atomic.Avalonia.Compact;

public abstract record SelectorExpression
{
    public SelectorExpression? Previous { get; set; }

    public abstract Expression ToExpressionCore(Expression parameter);

    public Expression ToExpression(Expression parameter)
    {
        if (Previous is not null)
            parameter = Previous.ToExpression(parameter);
        return ToExpressionCore(parameter);
    }

    public record PropertyEquals : SelectorExpression
    {
        private static readonly MethodInfo CallInfo;
        private static Dictionary<AvaloniaProperty, FieldInfo> Properties { get; } = [];

        static PropertyEquals()
        {
            CallInfo =
                typeof(Selectors).GetMethod(
                    nameof(Selectors.PropertyEquals),
                    [typeof(Selector), typeof(AvaloniaProperty), typeof(object)]
                ) ?? throw new InvalidOperationException();
        }

        public AvaloniaProperty Property { get; set; }
        public object? Value { get; set; }

        public PropertyEquals(
            SelectorExpression? previous,
            AvaloniaProperty property,
            object? value
        )
        {
            Previous = previous;
            Property = property;
            Value = value;
        }

        public override Expression ToExpressionCore(Expression parameter)
        {
            if (!Properties.TryGetValue(Property, out var prop))
            {
                foreach (var propInfo in Property.OwnerType.GetFields(BindingFlags.Static))
                {
                    if (propInfo.GetValue(null) != Property)
                        continue;
                    Properties[Property] = prop = propInfo;
                    break;
                }

                if (prop is null)
                    throw new InvalidOperationException();
            }

            var propAccess = Expression.MakeMemberAccess(null, prop);
            return Expression.Call(CallInfo, [parameter, propAccess, Expression.Constant(Value)]);
        }
    }

    public record Class : SelectorExpression
    {
        private static readonly MethodInfo CallInfo;

        static Class()
        {
            CallInfo =
                typeof(Selectors).GetMethod(
                    nameof(Selectors.Class),
                    [typeof(Selector), typeof(string)]
                ) ?? throw new InvalidOperationException();
        }

        public string? Name { get; set; }

        public Class(SelectorExpression? previous, string name)
        {
            Previous = previous;
            Name = name;
        }

        public override Expression ToExpressionCore(Expression parameter)
        {
            return Expression.Call(CallInfo, [parameter, Expression.Constant(Name)]);
        }
    }

    public record Is : SelectorExpression
    {
        public Type TargetType { get; set; }

        public Is(SelectorExpression? previous, Type targetType)
        {
            Previous = previous;
            TargetType = targetType;
        }

        public override Expression ToExpressionCore(Expression parameter)
        {
            return Expression.Call(
                typeof(Selectors),
                nameof(Selectors.Is),
                [TargetType],
                [parameter]
            );
        }
    }

    public record OfType : SelectorExpression
    {
        public Type TargetType { get; set; }

        public OfType(SelectorExpression? previous, Type targetType)
        {
            Previous = previous;
            TargetType = targetType;
        }

        public override Expression ToExpressionCore(Expression parameter)
        {
            return Expression.Call(
                typeof(Selectors),
                nameof(Selectors.OfType),
                [TargetType],
                [parameter]
            );
        }
    }
}

public static class SelectorsExpression
{
    public static SelectorExpression Class(this SelectorExpression? previous, string name) =>
        new SelectorExpression.Class(previous, name);

    public static SelectorExpression Is(this SelectorExpression? previous, Type type) =>
        new SelectorExpression.Is(previous, type);

    public static SelectorExpression Is<T>(this SelectorExpression? previous)
        where T : StyledElement => previous.Is(typeof(T));
}
