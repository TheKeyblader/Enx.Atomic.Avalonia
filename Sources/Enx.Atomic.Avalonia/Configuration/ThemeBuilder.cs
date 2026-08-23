namespace Enx.Atomic.Avalonia;

public class ThemeBuilder<TTheme>
    where TTheme : class, new()
{
    public static ThemeBuilder<TTheme> Create() => new ThemeBuilder<TTheme>();

    public TTheme Theme { get; } = new();

    public TTheme Build() => Theme;
}
