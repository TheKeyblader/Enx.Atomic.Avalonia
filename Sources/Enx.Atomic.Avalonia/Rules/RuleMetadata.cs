namespace Enx.Atomic.Avalonia;

/// <summary>Generation bookkeeping attached to every <see cref="IRule"/>, assigned by <see cref="AtomicGenerator{TTheme}"/>.</summary>
public class RuleMetadata
{
    /// <summary>Cache key for the rule's resolved output, if computed.</summary>
    internal string? Hash { get; set; }

    /// <summary>The rule's position within <see cref="AtomicConfiguration{TTheme}.Rules"/>, used to keep emitted styles in declaration order. <c>-1</c> until assigned.</summary>
    internal int Index { get; set; } = -1;
}