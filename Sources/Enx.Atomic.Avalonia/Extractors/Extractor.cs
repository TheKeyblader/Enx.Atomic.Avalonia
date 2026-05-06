namespace Enx.Atomic.Avalonia;

public abstract class Extractor
{
    public int Order { get; protected set; }
    public abstract void Extract(ExtractorContext context);
}