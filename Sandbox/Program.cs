using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Enx.Atomic.Avalonia;
using Enx.Atomic.Avalonia.Compact;
using Enx.Atomic.Avalonia.CSharp;

var configuration = new AtomicConfiguration<MiniTheme>
{
    Theme = new MiniTheme(),
    Rules =
    [
        new Rule.Static("hidden", [Visual.IsVisibleProperty.ToLiteral(false)]),
        new Rule.Static("visible", [Visual.IsVisibleProperty.ToLiteral(true)]),
        new Rule.Static("collapsed", [Visual.OpacityProperty.ToLiteral(0)]),
    ],
    Variants = [new VariantBreakpoints()],
};

var generator = new AtomicGenerator<MiniTheme>(configuration);
var result = generator.Generate(
    "sm:max-lg:hidden collapsed",
    new AtomicGenerator<MiniTheme>.Options()
);

var csharpEmitter = new CSharpEmitter<MiniTheme>(new CSharpEmitterOptions()
{

});

var content = csharpEmitter.Emit(new EmitContext<MiniTheme>()
{
    Configuration =  configuration,
    Utils = result
});
Console.WriteLine("Finish");

public class MiniTheme
{
    public string DefaultContainer { get; set; } = "main";

    [IsResourceDictionnary]
    public Dictionary<string, double> Breakpoints { get; set; } =
        new()
        {
            { "sm", 640 },
            { "md", 768 },
            { "lg", 1024 },
            { "xl", 1280 },
            { "2xl", 1536 },
        };

    [IsResourceDictionnary]
    public Dictionary<string, Color> Colors { get; set; } = [];
    [IsResourceDictionnary]
    public Dictionary<string, Color> ColorsDark { get; set; } = [];

    [IsResourceDictionnary] public int DefaultGridSize { get; set; } = 12;

    [IsResourceDictionnary] public ButtonTheme Button { get; set; } = new();
    
    public string Ignore { get; set; }
    
    public class ButtonTheme
    {
        public Thickness CompactMargin { get; set; }
        public Thickness Margin { get; set; }
    }
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
