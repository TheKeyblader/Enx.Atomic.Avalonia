namespace Enx.Atomic.Avalonia;

public record EmittedOuput
{
    public required string EmitterName { get; init; }
    public required EmitResult EmitResult { get; init; }
    public required string? WrittenTo { get; init; }
}