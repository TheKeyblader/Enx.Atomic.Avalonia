namespace Enx.Atomic.Avalonia;

/// <summary>Pairs an <see cref="EmitResult"/> with the emitter that produced it and the path it was written to, if any.</summary>
public record EmittedOuput
{
    /// <summary>The name (typically the type name) of the <see cref="IStyleEmitter{TTheme}"/> that produced this output.</summary>
    public required string EmitterName { get; init; }

    /// <summary>The generated file name and content.</summary>
    public required EmitResult EmitResult { get; init; }

    /// <summary>The absolute path the output was written to, or <see langword="null"/> if it was not persisted to disk.</summary>
    public required string? WrittenTo { get; init; }
}