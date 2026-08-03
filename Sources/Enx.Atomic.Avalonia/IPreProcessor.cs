namespace Enx.Atomic.Avalonia;

/// <summary>
/// Rewrites a raw utility token before it is matched against variants and rules, e.g. to expand
/// aliases or normalize shorthand syntax. Registered pre-processors run in order in
/// <see cref="AtomicConfiguration{TTheme}.PreProcessors"/>, each receiving the previous one's output.
/// </summary>
public interface IPreProcessor
{
    /// <summary>Transforms <paramref name="matcher"/> and returns the rewritten token, or <see langword="null"/> to leave it unchanged.</summary>
    string? Process(string matcher);
}
