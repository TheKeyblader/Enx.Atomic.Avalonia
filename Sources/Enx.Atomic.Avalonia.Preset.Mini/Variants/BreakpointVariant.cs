using Avalonia.Styling;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia.Preset.Mini.Variants;

public record VariantBreakpoints : VariantBase<MiniTheme>
{
    public VariantBreakpoints()
    {
        MultiPass = true;
    }

    public override VariantHandlerBase[] Match(string matcher, VariantContext<MiniTheme> context)
    {
        foreach (var (name, size) in context.Theme.Breakpoints)
        {
            var _matcher = matcher;
            var max = false;
            if (_matcher.StartsWith("max-"))
            {
                max = true;
                _matcher = matcher[4..];
            }

            if (!_matcher.StartsWith(name))
                continue;

            _matcher = _matcher[(name.Length + 1)..];

            return [new Handler(max, size) { Matcher = _matcher }];
        }

        return [];
    }

    private record Handler(bool Max, double Size) : VariantHandlerBase
    {
        public override VariantHandlerContext Handle(
            VariantHandlerContext input,
            Func<VariantHandlerContext, VariantHandlerContext> next
        )
        {
            VariantHandlerContext newInput;
            var @operator = StyleQueryComparisonOperator.GreaterThanOrEquals;
            if (Max)
                @operator = StyleQueryComparisonOperator.LessThan;

            StyleQueryExpression containerQuery;
            if (input.ContainerQuery is null)
                containerQuery = input.ContainerQuery.Width(@operator, Size);
            else
            {
                var widthQuery = input.ContainerQuery.Previous.Width(@operator, Size);

                containerQuery = input.ContainerQuery.Previous.And([
                    input.ContainerQuery,
                    widthQuery,
                ]);
            }

            return next(input with { ContainerQuery = containerQuery });
        }
    }
}
