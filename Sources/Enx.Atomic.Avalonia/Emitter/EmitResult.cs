namespace Enx.Atomic.Avalonia;

public record EmitResult
{
    public required string FileName { get; init; }
    public required string Content { get; init; }
}
