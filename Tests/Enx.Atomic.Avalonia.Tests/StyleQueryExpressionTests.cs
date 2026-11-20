using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Enx.Atomic.Avalonia.Compact;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// The container-query nodes Preset.Mini doesn't use itself (<c>Height</c>, <c>Or</c>, <c>And</c>; breakpoints only
/// emit <c>Width</c>) but which are public building blocks for third-party variants. Each is compiled to a real
/// <see cref="ContainerQuery"/> and checked against an actual headless container. Avalonia's value queries don't
/// implement <c>ToString</c>, so behaviour is the only thing worth asserting.
/// </summary>
public class StyleQueryExpressionTests
{
    private const string ContainerName = "query-test";

    [AvaloniaTheory]
    [InlineData(400, true)]
    [InlineData(200, false)]
    public void Height_MatchesOnTheContainersHeight(double height, bool expected)
    {
        var query = new StyleQueryExpression.Height(null, StyleQueryComparisonOperator.GreaterThanOrEquals, 300);

        Assert.Equal(expected, Applies(query, width: 800, height));
    }

    [AvaloniaTheory]
    [InlineData(800, 400, true)]
    [InlineData(500, 400, false)]
    [InlineData(800, 200, false)]
    public void And_RequiresEveryQuery(double width, double height, bool expected)
    {
        var query = new StyleQueryExpression.And(null, [WideEnough(), TallEnough()]);

        Assert.Equal(expected, Applies(query, width, height));
    }

    [AvaloniaTheory]
    [InlineData(500, 400, true)]
    [InlineData(800, 200, true)]
    [InlineData(500, 200, false)]
    public void Or_RequiresAnyQuery(double width, double height, bool expected)
    {
        var query = new StyleQueryExpression.Or(null, [WideEnough(), TallEnough()]);

        Assert.Equal(expected, Applies(query, width, height));
    }

    private static StyleQueryExpression.Width WideEnough() =>
        new(null, StyleQueryComparisonOperator.GreaterThanOrEquals, 600);

    private static StyleQueryExpression.Height TallEnough() =>
        new(null, StyleQueryComparisonOperator.GreaterThanOrEquals, 300);

    /// <summary>
    /// Shows a window of the given size whose content is a width-and-height container, with one style scoped to
    /// <paramref name="query"/> that tags a child <see cref="Border"/>, and reports whether the tag was applied.
    /// </summary>
    private static bool Applies(StyleQueryExpression query, double width, double height)
    {
        var parameter = Expression.Parameter(typeof(StyleQuery));
        var compiled = Expression
            .Lambda<Func<StyleQuery, StyleQuery>>(query.ToExpression(parameter), parameter)
            .Compile();

        var target = new Border();
        var container = new Panel { Children = { target } };
        Container.SetName(container, ContainerName);
        Container.SetSizing(container, ContainerSizing.WidthAndHeight);

        var style = new Style(selector => selector.OfType<Border>())
        {
            Setters = { new Setter(Control.TagProperty, "hit") },
        };
        var containerQuery = new ContainerQuery(previous => compiled(previous!), ContainerName)
        {
            Children = { style },
        };

        var window = new Window
        {
            Content = container,
            Width = width,
            Height = height,
        };
        window.Styles.Add(containerQuery);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            return Equals(target.Tag, "hit");
        }
        finally
        {
            window.Close();
        }
    }
}
