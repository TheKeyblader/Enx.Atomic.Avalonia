namespace Enx.Atomic.Avalonia;

/// <summary>Generation bookkeeping attached to every <see cref="IRule"/>, independent of its registration order.</summary>
public class RuleMetadata
{
    /// <summary>Cache key for the rule's resolved output, if computed.</summary>
    internal string? Hash { get; set; }
}
