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
    Variants = [],
};

var generator = new AtomicGenerator<MiniTheme>(configuration);

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
    public override VariantHandlerBase[] Matcher(string matcher, VariantContext<MiniTheme> context)
    {
        foreach (var (name, size) in context.Theme.Breakpoints)
        {
            var max = false;
            if (matcher.StartsWith("max-"))
            {
                max = true;
                matcher = matcher[4..];
            }


        }
    }

    public record Handler : VariantHandlerBase
    {
        public override VariantHandlerContext Handle(VariantHandlerContext input, Func<VariantHandlerContext, VariantHandlerContext> next)
        {

        }
    }
}
