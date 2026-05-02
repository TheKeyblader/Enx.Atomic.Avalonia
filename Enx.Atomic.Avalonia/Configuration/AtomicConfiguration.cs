namespace Enx.Atomic.Avalonia;

public class AtomicConfiguration<TTheme>
    where TTheme : class
{
    public List<Rule<TTheme>> Rules { get; set; } = [];
    public List<Extractor> Extractors { get; set; } = [new SplitExtractor()];
    public required TTheme Theme { get; set; }
}