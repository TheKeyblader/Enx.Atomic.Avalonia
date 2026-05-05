using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Styling;
using Enx.Atomic.Avalonia;

var configuration = new AtomicConfiguration<MiniTheme>
{
    Theme = new MiniTheme(),
    Rules =
    [
        new Rule<MiniTheme>.Static("hidden", [Visual.IsVisibleProperty.ToLiteral(false)]),
        new Rule<MiniTheme>.Static("visible", [Visual.IsVisibleProperty.ToLiteral(true)]),
        new Rule<MiniTheme>.Static("collapsed", [Visual.OpacityProperty.ToLiteral(0)]),
    ],
    Variants = [new VariantBreakpoints()],
};

var generator = new AtomicGenerator<MiniTheme>(configuration);
var result = generator.Generate("sm:max-lg:hidden collapsed", new AtomicGenerator<MiniTheme>.Options());
Console.WriteLine("Finish");

public class MiniTheme
{
    public string DefaultContainer { get; set; } = "main";

    public Dictionary<string, double> Breakpoints { get; set; } =
        new()
        {
            { "sm", 640 },
            { "md", 768 },
            { "lg", 1024 },
            { "xl", 1280 },
            { "2xl", 1536 },
        };
}

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

    public record Handler(bool Max, double Size) : VariantHandlerBase
    {
        public override VariantHandlerContext Handle(
            VariantHandlerContext input,
            Func<VariantHandlerContext, VariantHandlerContext> next
        )
        {
            Expression containerQuery;
            if (Max)
            {
                if (input.ContainerQuery is ParameterExpression)
                    containerQuery = Expression.Call(typeof(StyleQueries), nameof(StyleQueries.Width), Type.EmptyTypes, [input.ContainerQuery, Expression.Constant(StyleQueryComparisonOperator.LessThan), Expression.Constant(Size)]);
                else if(input.ContainerQuery is MethodCallExpression)
                    containerQuery = Expression.Call(typeof(StyleQueries))
            }

            var context = input with
            {
                ContainerQuery = Max
                    ? input.ContainerQuery.Width(
                        ,
                        Size
                    )
                    : input.ContainerQuery.Width(StyleQueryComparisonOperator.GreaterThanOrEquals, Size),
            };
            return next(context);
        }
    }
}
