namespace Enx.Atomic.Avalonia;

/// <summary>
/// Non-generic view of a <see cref="Themed{T}"/> value, so code that doesn't know <c>T</c> (e.g. the codegen
/// pipeline, working with a boxed <see cref="object"/>) can still tell whether it's looking at a themed value
/// and read both variants.
/// </summary>
public interface IThemedValue
{
    /// <summary><see langword="true"/> if <see cref="LightValue"/>/<see cref="DarkValue"/> were set explicitly to different values, rather than implicitly converted from a single, non-themed value.</summary>
    bool IsThemed { get; }

    /// <summary>The value for <see cref="Avalonia.Styling.ThemeVariant.Light"/>.</summary>
    object? LightValue { get; }

    /// <summary>The value for <see cref="Avalonia.Styling.ThemeVariant.Dark"/>.</summary>
    object? DarkValue { get; }
}

/// <summary>
/// A theme scale entry that may differ between <see cref="Avalonia.Styling.ThemeVariant.Light"/> and
/// <see cref="Avalonia.Styling.ThemeVariant.Dark"/> — e.g. a color scale's <c>Dictionary&lt;string, Themed&lt;IBrush&gt;&gt;</c>.
/// Implicitly convertible from a plain <typeparamref name="T"/> for the common case where a value doesn't
/// change between variants, so existing scale-seeding code (<c>theme.Colors[key] = someBrush</c>) keeps
/// compiling unchanged — only entries actually wrapped via the two-value constructor are treated as themed by
/// <see cref="IsThemed"/>, which is what decides whether a resource-based rule's value ends up in the plain
/// global resource dictionary or split across <c>ResourceDictionary.ThemeDictionaries</c>.
/// </summary>
public readonly record struct Themed<T> : IThemedValue
{
    /// <summary>The value for <see cref="Avalonia.Styling.ThemeVariant.Light"/>.</summary>
    public T Light { get; }

    /// <summary>The value for <see cref="Avalonia.Styling.ThemeVariant.Dark"/>.</summary>
    public T Dark { get; }

    /// <inheritdoc/>
    public bool IsThemed { get; }

    object? IThemedValue.LightValue => Light;
    object? IThemedValue.DarkValue => Dark;

    /// <summary>Creates an explicitly themed value, distinct for <see cref="Avalonia.Styling.ThemeVariant.Light"/>/<see cref="Avalonia.Styling.ThemeVariant.Dark"/>.</summary>
    public Themed(T light, T dark)
    {
        Light = light;
        Dark = dark;
        IsThemed = true;
    }

    private Themed(T value)
    {
        Light = value;
        Dark = value;
        IsThemed = false;
    }

    /// <summary>Wraps a single value used for both variants, marked <see cref="IsThemed"/> <see langword="false"/>.</summary>
    public static implicit operator Themed<T>(T value) => new(value);
}
