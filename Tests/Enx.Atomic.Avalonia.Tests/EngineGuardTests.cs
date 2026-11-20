using System.Linq.Expressions;
using Avalonia;
using Avalonia.Styling;
using Enx.Atomic.Avalonia.Compact;
using Enx.Atomic.Avalonia.Internal;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// The engine's misconfiguration guards: each must fail fast with a message naming what is wrong, and the variant
/// matcher's fork/loop bookkeeping must carry the state accumulated by earlier passes. Uses minimal hand-written
/// rules and variants rather than Preset.Mini, whose variants are never ambiguous or cyclic.
/// </summary>
public class EngineGuardTests
{
    [Fact]
    public void Constructor_WithRuleThatIsNeitherStaticNorDynamic_ThrowsNamingTheRule()
    {
        var configuration = new AtomicConfiguration<TestTheme> { Theme = new(), Rules = [new BogusRule()] };

        var exception = Assert.Throws<InvalidOperationException>(() => new AtomicGenerator<TestTheme>(configuration));

        Assert.Contains(typeof(BogusRule).FullName!, exception.Message);
    }

    [Fact]
    public void Constructor_WithShortcutThatIsNeitherStaticNorDynamic_ThrowsNamingTheShortcut()
    {
        // Valid shortcuts of both shapes first: the message must name the offending one, not the first one.
        var configuration = new AtomicConfiguration<TestTheme>
        {
            Theme = new(),
            Shortcuts =
            [
                Shortcut.Static("a", "b"),
                Shortcut.Dynamic<TestTheme>(new System.Text.RegularExpressions.Regex("^c$"), (_, _) => "d"),
                new BogusShortcut(),
            ],
        };

        var exception = Assert.Throws<InvalidOperationException>(() => new AtomicGenerator<TestTheme>(configuration));

        Assert.Contains(typeof(BogusShortcut).FullName!, exception.Message);
    }

    [Fact]
    public void MatchVariants_AmbiguousMultiPassVariant_ThrowsNamingTheVariantAndToken()
    {
        var generator = CreateGenerator(new AmbiguousVariant("b:") { MultiPass = true });

        var exception = Assert.Throws<InvalidOperationException>(() => generator.MatchVariants("b:x"));

        Assert.Contains(typeof(AmbiguousVariant).FullName!, exception.Message);
        Assert.Contains("'b:x'", exception.Message);
    }

    /// <summary>
    /// Regression: the 500-handler guard used to read a handler count captured before the matching loop, so it
    /// never tripped and a cyclic variant spun forever. The timeout turns a regression into a failure, not a hang.
    /// </summary>
    [Fact(Timeout = 30_000)]
    public async Task MatchVariants_CyclicMultiPassVariant_ThrowsInsteadOfLoopingForever()
    {
        var generator = CreateGenerator(new CyclicVariant());

        var exception = await Task.Run(() =>
            Assert.Throws<InvalidOperationException>(() => generator.MatchVariants("x"))
        );

        Assert.Contains("500", exception.Message);
        Assert.Contains("'x'", exception.Message);
    }

    /// <summary>
    /// Regression: an ambiguous (forking) variant used to fork from the state the call started with, silently
    /// dropping every variant matched by an earlier pass — here <c>a:</c> — from each branch.
    /// </summary>
    [Fact]
    public void MatchVariants_AmbiguousVariantAfterAnotherVariant_KeepsTheEarlierVariantInEveryBranch()
    {
        var prefix = new PrefixVariant("a:");
        var ambiguous = new AmbiguousVariant("b:");
        var generator = CreateGenerator(prefix, ambiguous);

        var results = generator.MatchVariants("a:b:x");

        Assert.Equal(2, results.Length);
        Assert.All(
            results,
            result =>
            {
                Assert.Equal("x", result.Current);
                Assert.Equal(2, result.Handlers.Length);
                Assert.Contains(prefix, result.Variants);
                Assert.Contains(ambiguous, result.Variants);
            }
        );
    }

    [Fact]
    public void MatchVariants_AmbiguousVariantKeepingTheToken_ForksWithTheTokenUnchanged()
    {
        var generator = CreateGenerator(new AmbiguousVariant("b:", Consume: false));

        var results = generator.MatchVariants("b:x");

        Assert.Equal(2, results.Length);
        Assert.All(
            results,
            result =>
            {
                Assert.Equal("b:x", result.Current);
                Assert.Single(result.Handlers);
            }
        );
    }

    [AvaloniaFact]
    public void PropertyEquals_WithPropertyNotDeclaredOnTheGivenType_ThrowsNamingTypeAndProperty()
    {
        var node = new SelectorExpression.PropertyEquals(null, Visual.IsVisibleProperty, true, typeof(string));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            node.ToExpression(Expression.Parameter(typeof(Selector)))
        );

        Assert.Contains(typeof(string).FullName!, exception.Message);
        Assert.Contains(Visual.IsVisibleProperty.Name, exception.Message);
    }

    [Fact]
    public void RequireStaticMethod_MissingOverload_ThrowsNamingTheSignature()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ReflectedApi.RequireStaticMethod(typeof(Selectors), "DoesNotExist", typeof(Selector), typeof(string))
        );

        Assert.Contains("Selectors.DoesNotExist(Selector, String)", exception.Message);
    }

    [Fact]
    public void RequireStaticMethod_ExistingOverload_ReturnsIt()
    {
        var method = ReflectedApi.RequireStaticMethod(
            typeof(Selectors),
            nameof(Selectors.Class),
            typeof(Selector),
            typeof(string)
        );

        Assert.Equal(nameof(Selectors.Class), method.Name);
    }

    private static AtomicGenerator<TestTheme> CreateGenerator(params VariantBase<TestTheme>[] variants) =>
        new(new AtomicConfiguration<TestTheme> { Theme = new(), Variants = [.. variants] });

    public sealed class TestTheme;

    private sealed class BogusRule : IRule
    {
        public RuleMetadata Metadata { get; } = new();
    }

    private sealed class BogusShortcut : IShortcut<TestTheme>;

    private sealed record PassThroughHandler : VariantHandlerBase
    {
        public override VariantHandlerContext Handle(
            VariantHandlerContext input,
            Func<VariantHandlerContext, VariantHandlerContext> next
        ) => next(input);
    }

    /// <summary>Strips <see cref="Prefix"/> unambiguously.</summary>
    private sealed record PrefixVariant(string Prefix) : VariantBase<TestTheme>
    {
        public override VariantHandlerBase[] Match(string matcher, VariantContext<TestTheme> context) =>
            matcher.StartsWith(Prefix, StringComparison.Ordinal)
                ? [new PassThroughHandler { Matcher = matcher[Prefix.Length..] }]
                : [];
    }

    /// <summary>
    /// Matches <see cref="Prefix"/> two different ways (two handlers), forcing a fork. With
    /// <see cref="Consume"/> off, its handlers leave the token unchanged (<c>Matcher = null</c>).
    /// </summary>
    private sealed record AmbiguousVariant(string Prefix, bool Consume = true) : VariantBase<TestTheme>
    {
        public override VariantHandlerBase[] Match(string matcher, VariantContext<TestTheme> context)
        {
            if (!matcher.StartsWith(Prefix, StringComparison.Ordinal))
                return [];

            var rest = Consume ? matcher[Prefix.Length..] : null;
            return
            [
                new PassThroughHandler { Matcher = rest, Order = 0 },
                new PassThroughHandler { Matcher = rest, Order = 1 },
            ];
        }
    }

    /// <summary>Matches every token without consuming anything, so a MultiPass loop never converges.</summary>
    private sealed record CyclicVariant : VariantBase<TestTheme>
    {
        public CyclicVariant() => MultiPass = true;

        public override VariantHandlerBase[] Match(string matcher, VariantContext<TestTheme> context) =>
            [new PassThroughHandler()];
    }
}
