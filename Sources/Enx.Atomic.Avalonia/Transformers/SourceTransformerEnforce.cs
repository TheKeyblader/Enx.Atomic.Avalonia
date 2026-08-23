namespace Enx.Atomic.Avalonia;

/// <summary>
/// Which stage a <see cref="ISourceTransformer{TTheme}"/> runs in relative to the others configured —
/// ported from UnoCSS's <c>SourceCodeTransformer.enforce</c>. Transformers run <see cref="Pre"/> group
/// first, then <see cref="Default"/>, then <see cref="Post"/>; within a group they run in
/// <see cref="AtomicConfiguration{TTheme}.Transformers"/> declaration order.
/// </summary>
public enum SourceTransformerEnforce
{
    Pre,
    Default,
    Post,
}
