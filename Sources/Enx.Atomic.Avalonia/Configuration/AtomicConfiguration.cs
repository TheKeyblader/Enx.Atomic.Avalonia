namespace Enx.Atomic.Avalonia;

public class AtomicConfiguration<TTheme>
    where TTheme : class
{
    public List<IPreProcessor> PreProcessors { get; set; } = [];
    public List<IRule> Rules { get; set; } = [];
    public List<Extractor> Extractors { get; set; } = [new SplitExtractor()];
    public List<VariantBase<TTheme>> Variants { get; set; } = [];
    public required TTheme Theme { get; set; }
    public List<ValueEmitter> Emitters { get; set; } =
    [
        new BasicTypeEmitter(),
        new ColorEmitter(),
        new SolidColorBrushEmitter(),
        new ThicknessEmitter(),
        new CornerRadiusEmitter(),
        new FontWeightEmitter(),
    ];
    public string Namespace { get; set; } = "Enx.Atomic";
    public string StyleClassName { get; set; } = "AtomicStyles";
    public string ContainerName { get; set; } = "top-level";
}