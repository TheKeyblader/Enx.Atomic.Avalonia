using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Styling;

namespace Enx.Atomic.Avalonia.Compact;

/// <summary>
/// A serializable, linked-list representation of an Avalonia <see cref="Avalonia.Styling.Selector"/> chain
/// (e.g. <c>Is&lt;Button&gt;().Class("hover")</c>), built up step by step by variant handlers and later
/// converted to a compiled <see cref="Expression"/> via <see cref="ToExpression(Expression)"/>. Existing as
/// data rather than a live expression lets rules and variants compose selectors without depending on
/// <see cref="System.Linq.Expressions"/> APIs directly.
/// </summary>
public abstract record SelectorExpression
{
    /// <summary>The previous link in the chain, applied before this one, or <see langword="null"/> if this is the first.</summary>
    public SelectorExpression? Previous { get; set; }

    /// <summary>Builds this node's contribution to the expression tree, given the already-built <paramref name="parameter"/> expression.</summary>
    public abstract Expression ToExpressionCore(Expression parameter);

    /// <summary>Builds the full expression tree for this chain, applying <see cref="Previous"/> first if present.</summary>
    public Expression ToExpression(Expression parameter)
    {
        if (Previous is not null)
            parameter = Previous.ToExpression(parameter);
        return ToExpressionCore(parameter);
    }

    /// <summary>Selects elements whose Avalonia property equals a given value, via <see cref="Selectors.PropertyEquals"/>.</summary>
    public record PropertyEquals : SelectorExpression
    {
        private static readonly MethodInfo CallInfo;
        private static Dictionary<(AvaloniaProperty Property, Type DeclaringType), FieldInfo> Properties { get; } = [];

        static PropertyEquals()
        {
            CallInfo =
                typeof(Selectors).GetMethod(
                    nameof(Selectors.PropertyEquals),
                    [typeof(Selector), typeof(AvaloniaProperty), typeof(object)]
                ) ?? throw new InvalidOperationException();
        }

        /// <summary>The property to compare.</summary>
        public AvaloniaProperty Property { get; set; }

        /// <summary>The value the property must equal.</summary>
        public object? Value { get; set; }

        /// <summary>
        /// The type whose public static fields are searched for <see cref="Property"/>'s declaring field, or
        /// <see langword="null"/> to use <see cref="Property"/>'s own <see cref="AvaloniaProperty.OwnerType"/>.
        /// Needed when <see cref="Property"/> was reached via <see cref="AvaloniaProperty{TValue}.AddOwner{TOwner}"/>
        /// from a type whose own field isn't public (or doesn't exist) — <c>OwnerType</c> always reports the
        /// type that originally registered the property, not the one it was accessed through.
        /// </summary>
        public Type? DeclaringType { get; set; }

        /// <summary>Creates a property-equality selector node.</summary>
        public PropertyEquals(
            SelectorExpression? previous,
            AvaloniaProperty property,
            object? value,
            Type? declaringType = null
        )
        {
            Previous = previous;
            Property = property;
            Value = value;
            DeclaringType = declaringType;
        }

        /// <inheritdoc/>
        public override Expression ToExpressionCore(Expression parameter)
        {
            var declaringType = DeclaringType ?? Property.OwnerType;
            var key = (Property, declaringType);
            if (!Properties.TryGetValue(key, out var prop))
            {
                foreach (var propInfo in declaringType.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (propInfo.GetValue(null) as AvaloniaProperty != Property)
                        continue;
                    Properties[key] = prop = propInfo;
                    break;
                }

                if (prop is null)
                    throw new InvalidOperationException();
            }

            var propAccess = Expression.MakeMemberAccess(null, prop);
            return Expression.Call(CallInfo, [parameter, propAccess, Expression.Constant(Value)]);
        }
    }

    /// <summary>Selects elements carrying a given style class, via <see cref="Selectors.Class"/>.</summary>
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

        /// <summary>The style class name to match.</summary>
        public string? Name { get; set; }

        /// <summary>Creates a class-selector node.</summary>
        public Class(SelectorExpression? previous, string name)
        {
            Previous = previous;
            Name = name;
        }

        /// <inheritdoc/>
        public override Expression ToExpressionCore(Expression parameter)
        {
            return Expression.Call(CallInfo, [parameter, Expression.Constant(Name)]);
        }
    }

    /// <summary>Selects elements assignable to a given type, via <see cref="Selectors.Is"/>.</summary>
    public record Is : SelectorExpression
    {
        /// <summary>The type elements must be assignable to.</summary>
        public Type TargetType { get; set; }

        /// <summary>Creates an "is-type" selector node.</summary>
        public Is(SelectorExpression? previous, Type targetType)
        {
            Previous = previous;
            TargetType = targetType;
        }

        /// <inheritdoc/>
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

    /// <summary>Selects elements of exactly a given type, via <see cref="Selectors.OfType"/>.</summary>
    public record OfType : SelectorExpression
    {
        /// <summary>The exact type elements must be.</summary>
        public Type TargetType { get; set; }

        /// <summary>Creates an "of-type" selector node.</summary>
        public OfType(SelectorExpression? previous, Type targetType)
        {
            Previous = previous;
            TargetType = targetType;
        }

        /// <inheritdoc/>
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

/// <summary>Fluent factory methods for chaining <see cref="SelectorExpression"/> nodes.</summary>
public static class SelectorsExpression
{
    /// <summary>Appends a <see cref="SelectorExpression.Class"/> node matching style class <paramref name="name"/>.</summary>
    public static SelectorExpression Class(this SelectorExpression? previous, string name) =>
        new SelectorExpression.Class(previous, name);

    /// <summary>Appends a <see cref="SelectorExpression.Is"/> node matching elements assignable to <paramref name="type"/>.</summary>
    public static SelectorExpression Is(this SelectorExpression? previous, Type type) =>
        new SelectorExpression.Is(previous, type);

    /// <summary>Appends a <see cref="SelectorExpression.Is"/> node matching elements assignable to <typeparamref name="T"/>.</summary>
    public static SelectorExpression Is<T>(this SelectorExpression? previous)
        where T : StyledElement => previous.Is(typeof(T));

    /// <summary>
    /// Appends a <see cref="SelectorExpression.PropertyEquals"/> node matching elements whose <paramref name="property"/>
    /// equals <paramref name="value"/>. See <see cref="SelectorExpression.PropertyEquals.DeclaringType"/> for when
    /// <paramref name="declaringType"/> is needed.
    /// </summary>
    public static SelectorExpression PropertyEquals(
        this SelectorExpression? previous,
        AvaloniaProperty property,
        object? value,
        Type? declaringType = null
    ) => new SelectorExpression.PropertyEquals(previous, property, value, declaringType);
}
