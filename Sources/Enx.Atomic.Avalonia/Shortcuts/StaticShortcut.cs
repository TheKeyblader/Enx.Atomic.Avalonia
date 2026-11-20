namespace Enx.Atomic.Avalonia.Shortcuts;

/// <summary>
/// The <see cref="IStaticShortcut"/> implementation <see cref="Shortcut.Static"/> returns. Kept internal — the
/// factory's interface return type is the extension point, not the concrete record.
/// </summary>
internal sealed record StaticShortcut : IStaticShortcut
{
    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string> Expansion { get; }

    public StaticShortcut(string name, string expansion)
    {
        Name = name;
        Expansion = ShortcutExpansion.Split(expansion);
    }
}
