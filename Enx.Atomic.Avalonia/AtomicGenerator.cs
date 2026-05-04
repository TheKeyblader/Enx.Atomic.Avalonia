namespace Enx.Atomic.Avalonia;

public class AtomicGenerator<TTheme>
    where TTheme : class
{
    public AtomicConfiguration<TTheme> Configuration { get; }

    public AtomicGenerator(AtomicConfiguration<TTheme> configuration)
    {
        Configuration = configuration;
        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        var index = 0;
        foreach (var rule in Configuration.Rules)
        {
            rule.Metadata.Index = index++;
        }
    }

    public HashSet<string> ApplyExtractors(string code, string? id = null, HashSet<string>? extracted = null)
    {
        extracted ??= [];

        var context = new ExtractorContext
        {
            RawCode = code,
            Code = code,
            Id = id,
            Extracted = extracted
        };

        foreach (var extractor in Configuration.Extractors.OrderBy(x => x.Order))
            extractor.Extract(context);

        return extracted;
    }

    public Result Generate(string input, Options options)
    {
        var tokens = ApplyExtractors(input, options.Id);
        return Generate(tokens, options);
    }

    public Result Generate(ISet<string> tokens, Options options)
    {
    }

    public class Options
    {
        public string? Id { get; init; }
    }

    public class Result
    {
    }
}