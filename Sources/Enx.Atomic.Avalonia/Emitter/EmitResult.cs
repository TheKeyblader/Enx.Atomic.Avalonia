namespace Enx.Atomic.Avalonia;

/// <summary>A single file produced by an <see cref="IStyleEmitter{TTheme}"/>.</summary>
public record EmitResult
{
    /// <summary>The suggested output file name.</summary>
    public required string FileName { get; init; }

    /// <summary>The generated file content.</summary>
    public required string Content { get; init; }
}
