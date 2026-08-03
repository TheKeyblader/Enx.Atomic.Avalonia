namespace Enx.Atomic.Avalonia;

/// <summary>The input an <see cref="IStyleEmitter{TTheme}"/> receives: the resolved styles to emit and the configuration they were generated from.</summary>
public record EmitContext<TTheme>
    where TTheme : class
{
    /// <summary>The configuration used to generate <see cref="Utils"/>, giving the emitter access to the theme and value emitters.</summary>
    public required AtomicConfiguration<TTheme> Configuration { get; init; }

    /// <summary>The resolved styles to emit.</summary>
    public required StringifiedUtil[] Utils { get; init; }
}