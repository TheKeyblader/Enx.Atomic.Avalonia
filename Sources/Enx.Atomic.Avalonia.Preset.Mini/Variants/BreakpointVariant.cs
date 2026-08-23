using Avalonia.Styling;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia.Preset.Mini.Variants;

/// <summary>
/// Matches a breakpoint prefix from <see cref="IBreakpointPart.Breakpoints"/> (<c>sm:</c>, <c>md:</c>, ...) as a
/// "container width is at least this breakpoint" query, or its <c>max-*:</c> counterpart (<c>max-sm:</c>, ...) as
/// "container width is below this breakpoint" — folded into the resolved style's <see cref="StyleQueryExpression"/>.
/// </summary>
public record BreakpointVariant<TTheme> : VariantBase<TTheme>
    where TTheme : class, IBreakpointPart
{
    public BreakpointVariant() => MultiPass = true;

    public override VariantHandlerBase[] Match(string matcher, VariantContext<TTheme> context)
    {
        foreach (var (name, size) in context.Theme.Breakpoints)
        {
            var maxPrefix = $"max-{name}:";
            if (matcher.StartsWith(maxPrefix, StringComparison.Ordinal))
            {
                return
                [
                    new BreakpointVariantHandler
                    {
                        Matcher = matcher[maxPrefix.Length..],
                        Operator = StyleQueryComparisonOperator.LessThanOrEquals,
                        Size = size,
                    },
                ];
            }

            var prefix = $"{name}:";
            if (matcher.StartsWith(prefix, StringComparison.Ordinal))
            {
                return
                [
                    new BreakpointVariantHandler
                    {
                        Matcher = matcher[prefix.Length..],
                        Operator = StyleQueryComparisonOperator.GreaterThanOrEquals,
                        Size = size,
                    },
                ];
            }
        }

        return [];
    }
}

/// <summary>Folds a container-width comparison into the resolved style's container query.</summary>
public record BreakpointVariantHandler : VariantHandlerBase
{
    public required StyleQueryComparisonOperator Operator { get; init; }
    public required double Size { get; init; }

    public override VariantHandlerContext Handle(
        VariantHandlerContext input,
        Func<VariantHandlerContext, VariantHandlerContext> next
    )
    {
        var result = next(input);
        return result with { ContainerQuery = result.ContainerQuery.Width(Operator, Size) };
    }
}
