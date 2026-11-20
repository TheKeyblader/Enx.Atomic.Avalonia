using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Enx.Atomic.Avalonia.Composites;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// The scenario suite for the composite side mechanism, written once and run twice: against the real runtime
/// hosts (<see cref="RuntimeCompositeScenarioTests"/>) and against the copy <c>StyleEmitter</c> embeds and
/// Roslyn compiles (<see cref="EmbeddedCompositeScenarioTests"/>). Two implementations of the same behavior is
/// the one thing that would silently drift in this design, so every scenario proves both — the assertions
/// below are the contract, not the runtime source.
///
/// <para>
/// Ported from the deleted <c>CompositeGhostSpikeTests</c> prototype; the findings recorded there (Style vs
/// StyleTrigger, set-then-dispose ordering, container query requirements) are kept as comments on the scenario
/// they explain.
/// </para>
/// </summary>
public abstract class CompositeScenarioTests
{
    /// <summary>
    /// Forces the implementation under test to register its class handlers before any assertion runs. Must be
    /// called first by every scenario — see each implementation's override.
    /// </summary>
    protected abstract bool EnsureInitialized();

    protected abstract SideProperties Sides(CompositeGroup group);

    private SideProperties Margin => Sides(CompositeGroup.Margin);

    private SideProperties Padding => Sides(CompositeGroup.Padding);

    private SideProperties BorderThickness => Sides(CompositeGroup.BorderThickness);

    private SideProperties CornerRadius => Sides(CompositeGroup.CornerRadius);

    /// <summary>
    /// Scenario 1: a base side token and a <c>:pointerover</c> variant of the same side are two ordinary styles,
    /// each writing only its own side property. The base style is declared first, so while hovered the variant's
    /// frame is the later of the two active frames at the same priority and wins; without hover only the base is
    /// active. This is the exact case the old ghost-property combiner got wrong (it merged both into one
    /// unconditional style).
    /// </summary>
    [AvaloniaFact]
    public void Scenario1_BaseAndHoverOnTheSameSide_SwitchWithPointerOver()
    {
        Assert.True(EnsureInitialized());
        var margin = Margin;

        var border = new Border { Classes = { "ml-2", "hover:ml-4" } };
        var window = new Window
        {
            Content = border,
            Styles =
            {
                SideStyle("ml-2", margin.Slot0, 8),
                SideStyle("hover:ml-4", margin.Slot0, 16, pseudoClass: ":pointerover"),
            },
        };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(8, 0, 0, 0), border.Margin);

            // Both sides are written by class-selected styles, which Avalonia frames at StyleTrigger (a class
            // selector is an activator), not at Style: this is why the two tokens tie on the side property itself.
            Assert.Equal(BindingPriority.StyleTrigger, border.GetDiagnostic(margin.Slot0).Priority);

            border.SetPseudoClass(":pointerover", true);
            Assert.Equal(new Thickness(16, 0, 0, 0), border.Margin);

            border.SetPseudoClass(":pointerover", false);
            Assert.Equal(new Thickness(8, 0, 0, 0), border.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Scenario 2: the same pair of styles declared in the opposite order.
    /// <para>
    /// <b>This documents the observed cascade, not a requirement.</b> Avalonia gives a style a
    /// <see cref="BindingPriority.StyleTrigger"/> frame as soon as its selector carries an activator — and a
    /// class selector is one (<c>StyleInstance.GetPriority</c>: <c>activator is not null</c> → StyleTrigger).
    /// Both <c>.ml-2</c> and <c>.hover:ml-4</c> are therefore at the <em>same</em> priority, so with no
    /// specificity to fall back on, the <em>last inserted frame</em> wins (<c>ValueStore.InsertFrame</c>
    /// appends, and <c>ReevaluateEffectiveValues</c> walks frames back-to-front). Declared first, the base
    /// style's frame is inserted last and keeps winning even while hovered: the variant never applies.
    /// </para>
    /// <para>
    /// Avalonia cannot be made to prefer the variant here — that is why the engine has to encode CSS's cascade
    /// into emission order (<c>StyleOrdering</c>, bug 8): base styles before pseudo-class variants, wider
    /// breakpoints before narrower ones. Per-side properties fix "one struct property overwritten by another";
    /// they cannot make a variant outrank its base by priority.
    /// </para>
    /// </summary>
    [AvaloniaFact]
    public void Scenario2_EqualPriorityStyles_LastDeclaredWins_ObservedCascade()
    {
        Assert.True(EnsureInitialized());
        var margin = Margin;

        // Control experiment, with no side properties and no handler involved: two plain class-selected styles
        // setting the same property are equally order-dependent. This proves the failure below is Avalonia's
        // cascade, not a bug in the mechanism.
        var plain = new Border { Classes = { "a", "b" } };
        var controlWindow = new Window
        {
            Content = plain,
            Styles =
            {
                new Style(x => x.Is<Border>().Class("b")) { Setters = { new Setter(Layoutable.WidthProperty, 20.0) } },
                new Style(x => x.Is<Border>().Class("a")) { Setters = { new Setter(Layoutable.WidthProperty, 10.0) } },
            },
        };
        controlWindow.Show();
        try
        {
            Assert.Equal(10, plain.Width);
        }
        finally
        {
            controlWindow.Close();
        }

        var border = new Border { Classes = { "ml-2", "hover:ml-4" } };
        var window = new Window
        {
            Content = border,
            Styles =
            {
                SideStyle("hover:ml-4", margin.Slot0, 16, pseudoClass: ":pointerover"),
                SideStyle("ml-2", margin.Slot0, 8),
            },
        };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(8, 0, 0, 0), border.Margin);

            border.SetPseudoClass(":pointerover", true);
            // Observed: stays (8,0,0,0). Both side styles are StyleTrigger (see the summary), the base style's
            // frame was inserted last and therefore wins the tie — the case StyleOrdering must prevent by never
            // emitting a base utility after a variant of it.
            Assert.Equal(new Thickness(8, 0, 0, 0), border.Margin);

            border.SetPseudoClass(":pointerover", false);
            Assert.Equal(new Thickness(8, 0, 0, 0), border.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Scenario 3 (the old overlapping-groups bug): one element uses three sides, a sibling only two. Because
    /// every side has its own property, neither element can clobber the other's sides, and the sibling keeps its
    /// own.
    /// </summary>
    [AvaloniaFact]
    public void Scenario3_SeveralSidesOnOneElement_AndASiblingWithASubset_KeepTheirOwnValues()
    {
        Assert.True(EnsureInitialized());
        var margin = Margin;

        var first = new Border { Classes = { "ml-1", "mr-2", "mt-3" } };
        var second = new Border { Classes = { "ml-1", "mr-2" } };
        var panel = new StackPanel { Children = { first, second } };
        var window = new Window
        {
            Content = panel,
            Styles =
            {
                SideStyle("ml-1", margin.Slot0, 4),
                SideStyle("mr-2", margin.Slot2, 8),
                SideStyle("mt-3", margin.Slot1, 12),
            },
        };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(4, 12, 8, 0), first.Margin);
            Assert.Equal(new Thickness(4, 0, 8, 0), second.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Scenario 4: a breakpoint variant is "just" a top-level <see cref="ContainerQuery"/>. The ancestor must be
    /// both named and declared as a container with a sizing mode, since Avalonia only installs the query provider
    /// (the thing that tracks the container size) when <see cref="Container.SizingProperty"/> is not
    /// <see cref="ContainerSizing.Normal"/>; the width used by the query is the constrained (available) size
    /// passed to the container during measure, not its arranged size.
    /// </summary>
    [AvaloniaFact]
    public void Scenario4_ContainerQuery_TogglesTheSideValueWhenTheContainerWidthCrossesTheBreakpoint()
    {
        Assert.True(EnsureInitialized());
        var margin = Margin;

        var border = new Border { Classes = { "sm:ml-4" } };
        var container = new Border { Child = border };
        Container.SetName(container, "top-level");
        Container.SetSizing(container, ContainerSizing.Width);

        var query = new ContainerQuery(q => q.Width(StyleQueryComparisonOperator.GreaterThanOrEquals, 640), "top-level")
        {
            Children = { SideStyle("sm:ml-4", margin.Slot0, 16) },
        };

        var window = new Window
        {
            Content = container,
            Styles = { query },
            Width = 500,
            Height = 400,
        };

        window.Show();
        try
        {
            // 500 < 640: the query does not match, so the side stays unset and no composition happens.
            Assert.Equal(default(Thickness), border.Margin);

            window.Width = 800;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new Thickness(16, 0, 0, 0), border.Margin);

            window.Width = 500;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(default(Thickness), border.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Scenario 5: a <see cref="ControlTheme"/> setter lives at Theme priority, below our Style-priority
    /// composition, so the composed padding wins while the utility class is present. Removing the class must
    /// make the side properties unset again and dispose our contribution, so the theme value shows through.
    /// </summary>
    [AvaloniaFact]
    public void Scenario5_ComposedValueBeatsTheControlTheme_AndTheThemeValueComesBackOnRemoval()
    {
        Assert.True(EnsureInitialized());
        var padding = Padding;

        var button = new Button
        {
            Classes = { "p-4" },
            Theme = new ControlTheme(typeof(Button))
            {
                Setters = { new Setter(TemplatedControl.PaddingProperty, new Thickness(1)) },
            },
        };
        var window = new Window
        {
            Content = button,
            Styles =
            {
                SideStyle("p-4", padding.Slot0, 16),
                SideStyle("p-4", padding.Slot1, 16),
                SideStyle("p-4", padding.Slot2, 16),
                SideStyle("p-4", padding.Slot3, 16),
            },
        };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(16), button.Padding);

            button.Classes.Remove("p-4");
            Assert.Equal(new Thickness(1), button.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Scenario 6: a plain user style that sets the real <see cref="Layoutable.MarginProperty"/> directly,
    /// combined with our side utilities on the same element, in both declaration orders. <b>Everything here
    /// records observed behavior, not a requirement.</b>
    /// <para>
    /// Observed: the user's class-selected style wins in <em>both</em> orders, because a style whose selector has
    /// an activator — a class selector is one — gets a <see cref="BindingPriority.StyleTrigger"/> frame
    /// (<c>StyleInstance.GetPriority</c>), while our composition is written at <see cref="BindingPriority.Style"/>.
    /// Priority beats frame order, so the styles' position in <c>Styles</c> is irrelevant.
    /// </para>
    /// <para>
    /// A type-only user style (<c>x.OfType&lt;Border&gt;()</c>, no class → no activator) stays at Style and
    /// genuinely ties with our write. Then frame order decides: <c>ValueStore.InsertFrame</c> appends
    /// equal-<c>FramePriority</c> frames, and <c>ReevaluateEffectiveValues</c> walks frames back-to-front, so our
    /// frame — created later, when the side handler runs — wins, giving 8.
    /// </para>
    /// </summary>
    [AvaloniaFact]
    public void Scenario6_UserStyleSettingTheRealProperty_ObservedOrdering()
    {
        Assert.True(EnsureInitialized());
        var margin = Margin;

        Style UserStyle() =>
            new(x => x.Is<Border>().Class("foo"))
            {
                Setters = { new Setter(Layoutable.MarginProperty, new Thickness(3)) },
            };

        // A Style instance can only belong to one Styles collection, so every window gets fresh instances.
        IStyle[] SideStyles() => [SideStyle("ml-2", margin.Slot0, 8)];

        // User style declared first, ours after it: the user style wins (StyleTrigger > Style).
        var userFirst = new Border { Classes = { "ml-2", "foo" } };
        var window = new Window { Content = userFirst, Styles = { UserStyle() } };
        foreach (var style in SideStyles())
            window.Styles.Add(style);
        window.Show();
        try
        {
            Assert.Equal(new Thickness(3), userFirst.Margin);
            Assert.Equal(BindingPriority.StyleTrigger, userFirst.GetDiagnostic(Layoutable.MarginProperty).Priority);
        }
        finally
        {
            window.Close();
        }

        // Ours declared first, user style after it: same result — priority, not declaration order, decides.
        var oursFirst = new Border { Classes = { "ml-2", "foo" } };
        var window2 = new Window { Content = oursFirst };
        foreach (var style in SideStyles())
            window2.Styles.Add(style);
        window2.Styles.Add(UserStyle());
        window2.Show();
        try
        {
            Assert.Equal(new Thickness(3), oursFirst.Margin);
        }
        finally
        {
            window2.Close();
        }

        // Type-only user style: no activator, so Style vs Style and our later frame wins the tie.
        var typeOnly = new Border { Classes = { "ml-2" } };
        var typeOnlyStyle = new Style(x => x.OfType<Border>())
        {
            Setters = { new Setter(Layoutable.MarginProperty, new Thickness(3)) },
        };
        var window3 = new Window { Content = typeOnly, Styles = { typeOnlyStyle } };
        foreach (var style in SideStyles())
            window3.Styles.Add(style);
        window3.Show();
        try
        {
            Assert.Equal(new Thickness(8, 0, 0, 0), typeOnly.Margin);
            Assert.Equal(BindingPriority.Style, typeOnly.GetDiagnostic(Layoutable.MarginProperty).Priority);
        }
        finally
        {
            window3.Close();
        }

        // A user *trigger* style on the real property is StyleTrigger priority too, so it beats our Style-priority
        // value while active — the expected precedence (StyleTrigger > Style), not an accident of ordering.
        var hovered = new Border { Classes = { "ml-2", "hover-foo" } };
        var hoverStyle = new Style(x => x.Is<Border>().Class("hover-foo").Class(":pointerover"))
        {
            Setters = { new Setter(Layoutable.MarginProperty, new Thickness(5)) },
        };
        var window4 = new Window { Content = hovered, Styles = { hoverStyle } };
        foreach (var style in SideStyles())
            window4.Styles.Add(style);
        window4.Show();
        try
        {
            Assert.Equal(new Thickness(8, 0, 0, 0), hovered.Margin);
            hovered.SetPseudoClass(":pointerover", true);
            Assert.Equal(new Thickness(5), hovered.Margin);
            hovered.SetPseudoClass(":pointerover", false);
            Assert.Equal(new Thickness(8, 0, 0, 0), hovered.Margin);
        }
        finally
        {
            window4.Close();
        }
    }

    /// <summary>
    /// Scenario 7: a local value is <see cref="BindingPriority.LocalValue"/>, above our composition, so setting
    /// <c>Margin</c> locally wins and clearing it reveals the composed value again (our entry was never removed).
    /// </summary>
    [AvaloniaFact]
    public void Scenario7_LocalValueWins_AndClearingItRevealsTheComposedValue()
    {
        Assert.True(EnsureInitialized());
        var margin = Margin;

        var border = new Border { Classes = { "ml-2" } };
        var window = new Window { Content = border, Styles = { SideStyle("ml-2", margin.Slot0, 8) } };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(8, 0, 0, 0), border.Margin);

            border.Margin = new Thickness(10);
            Assert.Equal(new Thickness(10), border.Margin);

            border.ClearValue(Layoutable.MarginProperty);
            Assert.Equal(new Thickness(8, 0, 0, 0), border.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Scenario 8: removing the utility class detaches its style frame, every side falls back to <c>NaN</c>, and
    /// the handler must dispose its contribution so no Style-priority value is left behind. Checked through the
    /// property value, <see cref="AvaloniaObject.IsSet"/>, and the diagnostic priority (Unset once truly gone).
    /// </summary>
    [AvaloniaFact]
    public void Scenario8_RemovingTheClass_RemovesTheComposedContributionEntirely()
    {
        Assert.True(EnsureInitialized());
        var margin = Margin;

        var border = new Border { Classes = { "ml-2" } };
        var window = new Window { Content = border, Styles = { SideStyle("ml-2", margin.Slot0, 8) } };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(8, 0, 0, 0), border.Margin);
            Assert.Equal(BindingPriority.Style, border.GetDiagnostic(Layoutable.MarginProperty).Priority);

            border.Classes.Remove("ml-2");

            Assert.Equal(default(Thickness), border.Margin);
            Assert.False(border.IsSet(Layoutable.MarginProperty));
            Assert.Equal(BindingPriority.Unset, border.GetDiagnostic(Layoutable.MarginProperty).Priority);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Scenario 9: the same side setter must reach the AddOwner'ed <c>Padding</c> of every control that registers
    /// it (<see cref="TemplatedControl"/>, <see cref="Decorator"/>, <see cref="TextBlock"/>, ...) and must be a
    /// no-op on a control where the property is not registered at all (<see cref="Canvas"/>).
    /// </summary>
    [AvaloniaFact]
    public void Scenario9_PaddingSidesReachEveryAddOwner_AndAreANoOpWherePaddingIsNotRegistered()
    {
        Assert.True(EnsureInitialized());
        var padding = Padding;

        var button = new Button { Classes = { "p-4" } };
        var border = new Border { Classes = { "p-4" } };
        var textBlock = new TextBlock { Classes = { "p-4" } };
        var canvas = new Canvas { Classes = { "p-4" } };
        var panel = new StackPanel { Children = { button, border, textBlock, canvas } };
        var window = new Window
        {
            Content = panel,
            Styles =
            {
                SideStyle("p-4", padding.Slot0, 16),
                SideStyle("p-4", padding.Slot1, 16),
                SideStyle("p-4", padding.Slot2, 16),
                SideStyle("p-4", padding.Slot3, 16),
            },
        };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(16), button.Padding);
            Assert.Equal(new Thickness(16), border.Padding);
            Assert.Equal(new Thickness(16), textBlock.Padding);

            // Canvas registers no Padding: the handler bails out instead of setting a value nobody reads.
            Assert.False(AvaloniaPropertyRegistry.Instance.IsRegistered(canvas, Decorator.PaddingProperty));
            Assert.False(canvas.IsSet(Decorator.PaddingProperty));
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Scenario 10: a uniform token is four setters in one style, so the handler runs once per side change and the
    /// real property is composed four times inside a single styling pass. The final value is correct and the
    /// intermediate values only ever grow towards it (set-then-dispose never lets the value fall back to the
    /// default between two compositions — dispose-then-set would have produced seven notifications with resets).
    /// </summary>
    [AvaloniaFact]
    public void Scenario10_UniformTokenWithFourSetters_ComposesTheCorrectFinalValueMonotonically()
    {
        Assert.True(EnsureInitialized());
        var margin = Margin;

        var border = new Border { Classes = { "m-4" } };
        var changes = new List<Thickness>();
        border.PropertyChanged += (_, e) =>
        {
            if (e.Property == Layoutable.MarginProperty)
                changes.Add(border.Margin);
        };

        var quadStyle = new Style(x => x.Is<Control>().Class("m-4"))
        {
            Setters =
            {
                new Setter(margin.Slot0, 16.0),
                new Setter(margin.Slot1, 16.0),
                new Setter(margin.Slot2, 16.0),
                new Setter(margin.Slot3, 16.0),
            },
        };
        var window = new Window { Content = border, Styles = { quadStyle } };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(16), border.Margin);
            Assert.Equal(new Thickness(16), changes[^1]);

            // Four side changes, four compositions: (16,0,0,0), (16,16,0,0), (16,16,16,0), (16,16,16,16).
            Assert.Equal(4, changes.Count);
            Assert.All(changes, value => Assert.True(value.Left == 16));

            for (var i = 1; i < changes.Count; i++)
            {
                Assert.True(changes[i].Top >= changes[i - 1].Top);
                Assert.True(changes[i].Right >= changes[i - 1].Right);
                Assert.True(changes[i].Bottom >= changes[i - 1].Bottom);
            }
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Scenario 11: all four shipped composites on one element, with a distinct value per slot, then full removal.
    /// The corner radius assertions are the slot-order guard: <see cref="CornerRadius"/>'s four-argument
    /// constructor is TopLeft/TopRight/BottomRight/BottomLeft, unlike <see cref="Thickness"/>'s
    /// Left/Top/Right/Bottom, and a host that used the flat order would swap two corners here.
    /// </summary>
    [AvaloniaFact]
    public void Scenario11_AllFourComposites_ComposeAndFallBack()
    {
        Assert.True(EnsureInitialized());
        var margin = Margin;
        var padding = Padding;
        var borderThickness = BorderThickness;
        var cornerRadius = CornerRadius;

        var border = new Border { Classes = { "sides-margin", "sides-padding", "sides-thickness", "sides-corner" } };
        var window = new Window
        {
            Content = border,
            Styles =
            {
                QuadStyle("sides-margin", margin, 1, 2, 3, 4),
                QuadStyle("sides-padding", padding, 5, 6, 7, 8),
                QuadStyle("sides-thickness", borderThickness, 9, 10, 11, 12),
                QuadStyle("sides-corner", cornerRadius, 13, 14, 15, 16),
            },
        };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(1, 2, 3, 4), border.Margin);
            Assert.Equal(new Thickness(5, 6, 7, 8), border.Padding);
            Assert.Equal(new Thickness(9, 10, 11, 12), border.BorderThickness);
            Assert.Equal(new CornerRadius(13, 14, 15, 16), border.CornerRadius);

            border.Classes.Remove("sides-margin");
            border.Classes.Remove("sides-padding");
            border.Classes.Remove("sides-thickness");
            border.Classes.Remove("sides-corner");

            Assert.Equal(default(Thickness), border.Margin);
            Assert.Equal(default(Thickness), border.Padding);
            Assert.Equal(default(Thickness), border.BorderThickness);
            Assert.Equal(default(CornerRadius), border.CornerRadius);
            Assert.False(border.IsSet(Layoutable.MarginProperty));
            Assert.False(border.IsSet(Decorator.PaddingProperty));
            Assert.False(border.IsSet(Border.BorderThicknessProperty));
            Assert.False(border.IsSet(Border.CornerRadiusProperty));
        }
        finally
        {
            window.Close();
        }
    }

    private static Style SideStyle(
        string className,
        AttachedProperty<double> property,
        double value,
        string? pseudoClass = null
    )
    {
        return new Style(x =>
        {
            var selector = x.Is<Control>().Class(className);
            return pseudoClass is null ? selector : selector.Class(pseudoClass);
        })
        {
            Setters = { new Setter(property, value) },
        };
    }

    private static Style QuadStyle(
        string className,
        SideProperties sides,
        double first,
        double second,
        double third,
        double fourth
    )
    {
        return new Style(x => x.Is<Control>().Class(className))
        {
            Setters =
            {
                new Setter(sides.Slot0, first),
                new Setter(sides.Slot1, second),
                new Setter(sides.Slot2, third),
                new Setter(sides.Slot3, fourth),
            },
        };
    }
}

/// <summary>The scenario suite against the real host classes in <c>Enx.Atomic.Avalonia.Composites</c>.</summary>
public class RuntimeCompositeScenarioTests : CompositeScenarioTests
{
    protected override bool EnsureInitialized() => RuntimeCompositeHosts.EnsureInitialized();

    protected override SideProperties Sides(CompositeGroup group) => RuntimeCompositeHosts.Sides(group);
}

/// <summary>The scenario suite against the self-contained copy the emitter produces and Roslyn compiles.</summary>
public class EmbeddedCompositeScenarioTests : CompositeScenarioTests
{
    protected override bool EnsureInitialized()
    {
        // Compiles the emitted source and loads the assembly on first use; the returned instance's property
        // instances are the embedded host's own, so every scenario below exercises the generated code.
        _ = EmbeddedCompositeHosts.Instance;
        return true;
    }

    protected override SideProperties Sides(CompositeGroup group) => EmbeddedCompositeHosts.Instance.Sides(group);
}
