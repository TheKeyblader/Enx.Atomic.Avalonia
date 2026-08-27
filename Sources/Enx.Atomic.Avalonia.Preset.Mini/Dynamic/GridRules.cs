using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Preset.Mini.Dynamic;

/// <summary>
/// Ghost attached properties syncing into <see cref="Grid.ColumnDefinitions"/>/<see cref="Grid.RowDefinitions"/>,
/// which aren't backed by a real <see cref="AvaloniaProperty"/> (they're plain CLR properties over an internal
/// field) and so can't be targeted by a <see cref="Avalonia.Styling.Setter"/> directly. Adapted from
/// https://github.com/AvaloniaUI/Avalonia/discussions/14340#discussioncomment-8233251: a real attached property
/// carries the desired definitions, and a class handler syncs them into the real collection whenever it changes.
/// Definitions are cloned rather than assigned directly — a <see cref="ColumnDefinition"/>/<see cref="RowDefinition"/>
/// can only belong to one collection's <c>Parent</c> at a time.
///
/// <para>
/// Marked <see cref="EmittableGhostPropertyHostAttribute"/>: unlike <see cref="SpecialProperties"/>, this
/// class's own compiled code is what actually applies the effect (the class handler below), so a project only
/// consuming the build-time codegen output — which may not reference this assembly at all — needs an
/// equivalent copy of it. <see cref="EmbeddableSource"/> is that copy, kept in sync by hand: same field names,
/// same behavior, just <c>global::</c>-qualified throughout and calling <c>AddClassHandler</c> by its static
/// method form instead of as an extension, so it needs no <c>using</c> directives of its own.
/// </para>
/// </summary>
[EmittableGhostPropertyHost]
public static class GridDefinitions
{
    public static readonly AttachedProperty<ColumnDefinitions?> ColumnDefinitionsProperty =
        AvaloniaProperty.RegisterAttached<Grid, ColumnDefinitions?>("EnxColumnDefinitions", typeof(GridDefinitions));

    public static readonly AttachedProperty<RowDefinitions?> RowDefinitionsProperty = AvaloniaProperty.RegisterAttached<
        Grid,
        RowDefinitions?
    >("EnxRowDefinitions", typeof(GridDefinitions));

    /// <summary>Kept in sync by hand with this class's own body — see the type's remarks.</summary>
    public const string EmbeddableSource = """
        file static class GridDefinitions
        {
            public static readonly global::Avalonia.AttachedProperty<global::Avalonia.Controls.ColumnDefinitions?> ColumnDefinitionsProperty =
                global::Avalonia.AvaloniaProperty.RegisterAttached<global::Avalonia.Controls.Grid, global::Avalonia.Controls.ColumnDefinitions?>("EnxColumnDefinitions", typeof(GridDefinitions));

            public static readonly global::Avalonia.AttachedProperty<global::Avalonia.Controls.RowDefinitions?> RowDefinitionsProperty =
                global::Avalonia.AvaloniaProperty.RegisterAttached<global::Avalonia.Controls.Grid, global::Avalonia.Controls.RowDefinitions?>("EnxRowDefinitions", typeof(GridDefinitions));

            static GridDefinitions()
            {
                global::Avalonia.AvaloniaObjectExtensions.AddClassHandler<global::Avalonia.Controls.Grid, global::Avalonia.Controls.ColumnDefinitions?>(
                    ColumnDefinitionsProperty.Changed,
                    (grid, e) =>
                    {
                        grid.ColumnDefinitions.Clear();
                        var newValue = e.NewValue.GetValueOrDefault();
                        if (newValue is not null)
                        {
                            foreach (var c in newValue)
                                grid.ColumnDefinitions.Add(new global::Avalonia.Controls.ColumnDefinition(c.Width));
                        }
                    }
                );

                global::Avalonia.AvaloniaObjectExtensions.AddClassHandler<global::Avalonia.Controls.Grid, global::Avalonia.Controls.RowDefinitions?>(
                    RowDefinitionsProperty.Changed,
                    (grid, e) =>
                    {
                        grid.RowDefinitions.Clear();
                        var newValue = e.NewValue.GetValueOrDefault();
                        if (newValue is not null)
                        {
                            foreach (var r in newValue)
                                grid.RowDefinitions.Add(new global::Avalonia.Controls.RowDefinition(r.Height));
                        }
                    }
                );
            }
        }
        """;
}

/// <summary>
/// Dynamic rule setting <see cref="Grid.ColumnDefinitions"/> to <c>n</c> equal (<c>1*</c>) columns (<c>grid-cols-{n}</c>),
/// via the <see cref="GridDefinitions.ColumnDefinitionsProperty"/> ghost property. Structural, not theme-driven —
/// <c>n</c> is parsed directly, not looked up in a scale.
/// </summary>
public partial class GridColumnsRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!int.TryParse(match.Groups["n"].Value, out var n) || n <= 0)
            return [];

        var columns = new ColumnDefinitions(string.Join(',', Enumerable.Repeat("*", n)));
        return [GridDefinitions.ColumnDefinitionsProperty.ToLiteral(columns, typeof(Grid))];
    }

    [GeneratedRegex("^grid-cols-(?<n>\\d+)$")]
    private static partial Regex CompiledRegex();
}

/// <summary>
/// Dynamic rule setting <see cref="Grid.RowDefinitions"/> to <c>n</c> equal (<c>1*</c>) rows (<c>grid-rows-{n}</c>) —
/// see <see cref="GridColumnsRule{TTheme}"/>.
/// </summary>
public partial class GridRowsRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!int.TryParse(match.Groups["n"].Value, out var n) || n <= 0)
            return [];

        var rows = new RowDefinitions(string.Join(',', Enumerable.Repeat("*", n)));
        return [GridDefinitions.RowDefinitionsProperty.ToLiteral(rows, typeof(Grid))];
    }

    [GeneratedRegex("^grid-rows-(?<n>\\d+)$")]
    private static partial Regex CompiledRegex();
}

/// <summary>
/// Dynamic rule setting <see cref="Grid.ColumnProperty"/>/<see cref="Grid.RowProperty"/> (<c>col-{n}</c>,
/// <c>row-{n}</c>, 0-based) and <see cref="Grid.ColumnSpanProperty"/>/<see cref="Grid.RowSpanProperty"/>
/// (<c>col-span-{n}</c>, <c>row-span-{n}</c>) — real attached properties on <see cref="Grid"/>'s children, no
/// ghost property needed.
/// </summary>
public partial class GridCellRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!int.TryParse(match.Groups["n"].Value, out var n) || n < 0)
            return [];

        var span = match.Groups["span"].Success;
        return match.Groups["axis"].Value switch
        {
            "col" when span && n > 0 => [Grid.ColumnSpanProperty.ToLiteral(n)],
            "row" when span && n > 0 => [Grid.RowSpanProperty.ToLiteral(n)],
            "col" when !span => [Grid.ColumnProperty.ToLiteral(n)],
            "row" when !span => [Grid.RowProperty.ToLiteral(n)],
            _ => [],
        };
    }

    [GeneratedRegex("^(?<axis>col|row)-(?<span>span-)?(?<n>\\d+)$")]
    private static partial Regex CompiledRegex();
}
