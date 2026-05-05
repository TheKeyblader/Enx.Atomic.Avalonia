using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Styling;
using Enx.Atomic.Avalonia;
using Enx.Atomic.Avalonia.Compact;

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
var result = generator.Generate(
    "sm:max-lg:hidden collapsed",
    new AtomicGenerator<MiniTheme>.Options()
);
var content = StyleEmitter.Generate(configuration, result);
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
            VariantHandlerContext newInput;
            if (Max)
            {
                if (input.ContainerQuery is null)
                {
                    newInput = input with
                    {
                        ContainerQuery = input.ContainerQuery.Width(
                            StyleQueryComparisonOperator.LessThan,
                            Size
                        ),
                    };
                }
                else
                {
                    var widthQuery = input.ContainerQuery.Previous.Width(
                        StyleQueryComparisonOperator.LessThan,
                        Size
                    );

                    newInput = input with
                    {
                        ContainerQuery = input.ContainerQuery.Previous.And([
                            input.ContainerQuery,
                            widthQuery,
                        ]),
                    };
                }
            }
            else
            {
                if (input.ContainerQuery is null)
                {
                    newInput = input with
                    {
                        ContainerQuery = input.ContainerQuery.Width(
                            StyleQueryComparisonOperator.GreaterThanOrEquals,
                            Size
                        ),
                    };
                }
                else
                {
                    var widthQuery = input.ContainerQuery.Previous.Width(
                        StyleQueryComparisonOperator.GreaterThanOrEquals,
                        Size
                    );

                    newInput = input with
                    {
                        ContainerQuery = input.ContainerQuery.Previous.And([
                            input.ContainerQuery,
                            widthQuery,
                        ]),
                    };
                }
            }

            return next(newInput);
        }
    }
}
