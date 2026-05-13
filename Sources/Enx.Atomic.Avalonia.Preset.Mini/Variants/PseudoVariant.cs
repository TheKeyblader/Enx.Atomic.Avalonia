using System.Collections.Immutable;
using System.Reflection;
using Avalonia.Controls.Metadata;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia.Preset.Mini.Variants;

public record PseudoVariant : VariantBase<MiniTheme>
{
    private static readonly ImmutableArray<string> _pseudos;

    static PseudoVariant()
    {
        _pseudos = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetExportedTypes())
            .SelectMany(t => t.GetCustomAttributes<PseudoClassesAttribute>())
            .SelectMany(p => p.PseudoClasses)
            .Distinct()
            .Select(s => s[1..])
            .ToImmutableArray();
    }

    public PseudoVariant()
    {
        MultiPass = true;
    }

    public override VariantHandlerBase[] Match(string matcher, VariantContext<MiniTheme> context)
    {
        foreach (var pseudo in _pseudos)
        {
            var _matcher = matcher;
            if (!_matcher.StartsWith(pseudo + ":"))
                continue;

            _matcher = _matcher[(pseudo.Length + 1)..];

            return [new Handler(pseudo) { Matcher = _matcher }];
        }

        return [];
    }

    private record Handler(string Pseudo) : VariantHandlerBase
    {
        public override VariantHandlerContext Handle(
            VariantHandlerContext input,
            Func<VariantHandlerContext, VariantHandlerContext> next
        )
        {
            return next(input with { Selector = input.Selector.Class(":" + Pseudo) });
        }
    }
}
