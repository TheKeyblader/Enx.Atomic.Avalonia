namespace Enx.Atomic.Avalonia;

/// <summary>Pulls candidate utility tokens out of raw source text. Configured extractors run in <see cref="Order"/> against the same <see cref="ExtractorContext"/>.</summary>
public abstract class Extractor
{
    /// <summary>Relative order this extractor runs in among <see cref="AtomicConfiguration{TTheme}.Extractors"/>; lower values run first.</summary>
    public int Order { get; protected set; }

    /// <summary>Scans <see cref="ExtractorContext.Code"/> and adds any candidate tokens found to <see cref="ExtractorContext.Extracted"/>.</summary>
    public abstract void Extract(ExtractorContext context);
}