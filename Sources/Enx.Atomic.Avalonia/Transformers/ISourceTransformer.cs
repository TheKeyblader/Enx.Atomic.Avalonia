namespace Enx.Atomic.Avalonia;

/// <summary>
/// Rewrites a source file's <em>full text</em> before tokens are extracted from it — ported from UnoCSS's
/// <c>SourceCodeTransformer</c>. This is the tool for anything that needs to see several tokens together
/// (e.g. combining co-occurring utilities into one), unlike <see cref="IPreProcessor"/>, which only ever
/// sees one already-isolated token in front of it.
/// </summary>
public interface ISourceTransformer<TTheme>
    where TTheme : class
{
    /// <summary>Name for diagnostics, mirroring UnoCSS's <c>SourceCodeTransformer.name</c>.</summary>
    string Name { get; }

    /// <summary>Which stage this transformer runs in — see <see cref="SourceTransformerEnforce"/>.</summary>
    SourceTransformerEnforce Enforce { get; }

    /// <summary>Cheap check on the source identifier; the transformer is skipped entirely when this returns <see langword="false"/>. <see langword="null"/> means every source is considered.</summary>
    Func<string, bool>? IdFilter { get; }

    /// <summary>Cheap check on the current code, evaluated before doing any real work — mirrors UnoCSS's <c>codeFilter</c>, meant to let a transformer bail out without scanning/allocating.</summary>
    Func<string, string?, bool>? CodeFilter { get; }

    /// <summary>
    /// Rewrites <paramref name="code"/> and returns the result (or <paramref name="code"/> itself if
    /// unchanged). <paramref name="generator"/> is available for transformers that need to resolve a
    /// candidate token against the configured rules to decide whether/how to rewrite it.
    /// </summary>
    string Transform(string code, string? id, AtomicGenerator<TTheme> generator);
}
