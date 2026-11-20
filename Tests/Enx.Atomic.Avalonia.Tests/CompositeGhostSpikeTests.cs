using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// Prototype spike for the replacement of the build-time <c>GhostPropertyCombiner</c>: per-side utilities write
/// four <see cref="double"/> attached properties (default <see cref="double.NaN"/> meaning "this side is unset")
/// and a class handler recomposes the real composite property (<see cref="Thickness"/>) as a normal style value.
/// Everything below lives in the test project on purpose — nothing under <c>Sources/</c> is touched; this file
/// only answers "does Avalonia let this design work, and with which wrinkles".
/// </summary>
public class CompositeGhostSpikeTests
{
    // Touching this field runs the host's static constructor (property registration + class handlers) before the
    // first test method body executes, so no test can observe an unhandled side-property change.
    private static readonly bool s_hostInitialized = CompositeGhostSpikeHost.EnsureInitialized();

    /// <summary>
    /// Scenario 1: a base side token and a <c>:pointerover</c> variant of the same side are two ordinary styles, each
    /// writing only its own side property. The base style is declared first, so while hovered the variant's frame is
    /// the later of the two active frames at the same priority and wins; without hover only the base is active. This
    /// is the exact case the ghost-property combiner got wrong (it merged both into one unconditional style).
    /// </summary>
    [AvaloniaFact]
    public void Scenario1_BaseAndHoverOnTheSameSide_SwitchWithPointerOver()
    {
        Assert.True(s_hostInitialized);

        var border = new Border { Classes = { "ml-2", "hover:ml-4" } };
        var window = new Window
        {
            Content = border,
            Styles =
            {
                MarginSideStyle("ml-2", CompositeGhostSpikeHost.MarginLeftProperty, 8),
                MarginSideStyle(
                    "hover:ml-4",
                    CompositeGhostSpikeHost.MarginLeftProperty,
                    16,
                    pseudoClass: ":pointerover"
                ),
            },
        };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(8, 0, 0, 0), border.Margin);

            // Both sides are written by class-selected styles, which Avalonia frames at StyleTrigger (a class selector
            // is an activator), not at Style: this is why the two tokens tie on the side property itself.
            Assert.Equal(
                BindingPriority.StyleTrigger,
                border.GetDiagnostic(CompositeGhostSpikeHost.MarginLeftProperty).Priority
            );

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
    /// Scenario 2: the same pair of styles declared in the opposite order in <see cref="Window.Styles"/>.
    /// <para>
    /// <b>This documents the observed cascade, not a requirement.</b> Avalonia gives a style a
    /// <see cref="BindingPriority.StyleTrigger"/> frame as soon as its selector carries an activator — and a class
    /// selector is an activator (<c>StyleInstance.GetPriority</c>: <c>activator is not null</c> → StyleTrigger). Both
    /// <c>.ml-2</c> and <c>.hover:ml-4</c> are therefore at the <em>same</em> priority, so with no specificity to
    /// fall back on, the <em>last inserted frame</em> wins (<c>ValueStore.InsertFrame</c> appends, and
    /// <c>ReevaluateEffectiveValues</c> walks frames back-to-front). Declared first, the base style's frame is
    /// inserted last and keeps winning even while hovered: the variant never applies.
    /// </para>
    /// <para>
    /// Avalonia cannot be made to prefer the variant here — that is why the engine has to encode CSS's cascade into
    /// emission order (<c>StyleOrdering</c>, bug 8): base styles before pseudo-class variants, wider breakpoints
    /// before narrower ones. Per-side properties fix "one struct property overwritten by another"; they cannot make a
    /// variant outrank its base by priority.
    /// </para>
    /// </summary>
    [AvaloniaFact]
    public void Scenario2_EqualPriorityStyles_LastDeclaredWins_ObservedCascade()
    {
        // Control experiment, with no side properties and no handler involved: two plain class-selected styles setting
        // the same property are equally order-dependent. This proves the failure below is Avalonia's cascade, not a
        // bug in the spike.
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
                MarginSideStyle(
                    "hover:ml-4",
                    CompositeGhostSpikeHost.MarginLeftProperty,
                    16,
                    pseudoClass: ":pointerover"
                ),
                MarginSideStyle("ml-2", CompositeGhostSpikeHost.MarginLeftProperty, 8),
            },
        };

        window.Show();
        try
        {
            Assert.Equal(new Thickness(8, 0, 0, 0), border.Margin);

            border.SetPseudoClass(":pointerover", true);
            // Observed: stays (8,0,0,0). Both side styles are StyleTrigger (see the summary), the base style's frame
            // was inserted last and therefore wins the tie — this is the exact case StyleOrdering must prevent by
            // never emitting a base utility after a variant of it.
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
    /// Scenario 3 (the old overlapping-groups bug): one element uses three sides, a sibling only two. Because every
    /// side has its own property, neither element can clobber the other's sides, and the sibling keeps its own.
    /// </summary>
    [AvaloniaFact]
    public void Scenario3_SeveralSidesOnOneElement_AndA_SiblingWithASubset_KeepTheirOwnValues()
    {
        var first = new Border { Classes = { "ml-1", "mr-2", "mt-3" } };
        var second = new Border { Classes = { "ml-1", "mr-2" } };
        var panel = new StackPanel { Children = { first, second } };
        var window = new Window
        {
            Content = panel,
            Styles =
            {
                MarginSideStyle("ml-1", CompositeGhostSpikeHost.MarginLeftProperty, 4),
                MarginSideStyle("mr-2", CompositeGhostSpikeHost.MarginRightProperty, 8),
                MarginSideStyle("mt-3", CompositeGhostSpikeHost.MarginTopProperty, 12),
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
    /// Scenario 4: a breakpoint variant is "just" a top-level <see cref="ContainerQuery"/>. The ancestor must be both
    /// named and declared as a container with a sizing mode, since Avalonia only installs the query provider (the
    /// thing that tracks the container size) when <see cref="Container.SizingProperty"/> is not
    /// <see cref="ContainerSizing.Normal"/>; the width used by the query is the constrained (available) size passed
    /// to the container during measure, not its arranged size.
    /// </summary>
    [AvaloniaFact]
    public void Scenario4_ContainerQuery_TogglesTheSideValueWhenTheContainerWidthCrossesTheBreakpoint()
    {
        var border = new Border { Classes = { "sm:ml-4" } };
        var container = new Border { Child = border };
        Container.SetName(container, "top-level");
        Container.SetSizing(container, ContainerSizing.Width);

        var query = new ContainerQuery(q => q.Width(StyleQueryComparisonOperator.GreaterThanOrEquals, 640), "top-level")
        {
            Children = { MarginSideStyle("sm:ml-4", CompositeGhostSpikeHost.MarginLeftProperty, 16) },
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
    /// Scenario 5: a <see cref="ControlTheme"/> setter lives at Theme priority, below our Style-priority composition,
    /// so the composed padding wins while the utility class is present. Removing the class must make the side
    /// properties unset again and dispose our contribution, so the theme value shows through.
    /// </summary>
    [AvaloniaFact]
    public void Scenario5_ComposedValueBeatsTheControlTheme_AndTheThemeValueComesBackOnRemoval()
    {
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
                PaddingSideStyle(CompositeGhostSpikeHost.PaddingLeftProperty, 16),
                PaddingSideStyle(CompositeGhostSpikeHost.PaddingTopProperty, 16),
                PaddingSideStyle(CompositeGhostSpikeHost.PaddingRightProperty, 16),
                PaddingSideStyle(CompositeGhostSpikeHost.PaddingBottomProperty, 16),
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
    /// Scenario 6: a plain user style that sets the real <see cref="Layoutable.MarginProperty"/> directly, combined
    /// with our side utilities on the same element, in both declaration orders. <b>Everything here records observed
    /// behavior, not a requirement.</b>
    /// <para>
    /// Observed: the user's class-selected style wins in <em>both</em> orders (Margin 3), because a style whose
    /// selector has an activator — a class selector is one — gets a <see cref="BindingPriority.StyleTrigger"/> frame
    /// (<c>StyleInstance.GetPriority</c>), while our composition is written at <see cref="BindingPriority.Style"/>.
    /// Priority beats frame order, so the styles' position in <c>Styles</c> is irrelevant.
    /// </para>
    /// <para>
    /// A type-only user style (<c>x.OfType&lt;Border&gt;()</c>, no class → no activator) stays at Style and genuinely
    /// ties with our write. Then frame order decides: <c>ValueStore.InsertFrame</c> appends equal-<c>FramePriority</c>
    /// frames, and <c>ReevaluateEffectiveValues</c> walks frames back-to-front, so our frame — created later, when the
    /// side handler runs — wins, giving 8.
    /// </para>
    /// </summary>
    [AvaloniaFact]
    public void Scenario6_UserStyleSettingTheRealProperty_ObservedOrdering()
    {
        Style UserStyle() =>
            new(x => x.Is<Border>().Class("foo"))
            {
                Setters = { new Setter(Layoutable.MarginProperty, new Thickness(3)) },
            };

        // A Style instance can only belong to one Styles collection, so every window gets fresh instances.
        IStyle[] SideStyles() => [MarginSideStyle("ml-2", CompositeGhostSpikeHost.MarginLeftProperty, 8)];

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
        var border = new Border { Classes = { "ml-2" } };
        var window = new Window
        {
            Content = border,
            Styles = { MarginSideStyle("ml-2", CompositeGhostSpikeHost.MarginLeftProperty, 8) },
        };

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
    /// Scenario 8: removing the utility class detaches its style frame, every side falls back to <c>NaN</c>, and the
    /// handler must dispose its contribution so no Style-priority value is left behind. Checked through the property
    /// value, <see cref="AvaloniaObject.IsSet"/>, and the diagnostic priority (Unset once truly gone).
    /// </summary>
    [AvaloniaFact]
    public void Scenario8_RemovingTheClass_RemovesTheComposedContributionEntirely()
    {
        var border = new Border { Classes = { "ml-2" } };
        var window = new Window
        {
            Content = border,
            Styles = { MarginSideStyle("ml-2", CompositeGhostSpikeHost.MarginLeftProperty, 8) },
        };

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
    /// Scenario 9: the same side setter must reach the AddOwner'ed <c>Padding</c> of every control that registers it
    /// (<see cref="TemplatedControl"/>, <see cref="Decorator"/>, <see cref="TextBlock"/>, ...) and must be a no-op on a
    /// control where the property is not registered at all (<see cref="Canvas"/>).
    /// </summary>
    [AvaloniaFact]
    public void Scenario9_PaddingSidesReachEveryAddOwner_AndAreA_NoOpWherePaddingIsNotRegistered()
    {
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
                PaddingSideStyle(CompositeGhostSpikeHost.PaddingLeftProperty, 16),
                PaddingSideStyle(CompositeGhostSpikeHost.PaddingTopProperty, 16),
                PaddingSideStyle(CompositeGhostSpikeHost.PaddingRightProperty, 16),
                PaddingSideStyle(CompositeGhostSpikeHost.PaddingBottomProperty, 16),
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
    /// intermediate values only ever grow towards it (set-then-dispose never lets the value fall back to the default
    /// between two compositions). A production implementation may still want to coalesce these writes if the
    /// transient values are observable (layout invalidations, transitions, animations) — Avalonia gives no
    /// "end of styling pass" hook, so coalescing means deferring to a dispatcher turn.
    /// </summary>
    [AvaloniaFact]
    public void Scenario10_UniformTokenWithFourSetters_ComposesTheCorrectFinalValue()
    {
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
                new Setter(CompositeGhostSpikeHost.MarginLeftProperty, 16.0),
                new Setter(CompositeGhostSpikeHost.MarginTopProperty, 16.0),
                new Setter(CompositeGhostSpikeHost.MarginRightProperty, 16.0),
                new Setter(CompositeGhostSpikeHost.MarginBottomProperty, 16.0),
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

    private static Style MarginSideStyle(
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

    private static Style PaddingSideStyle(AttachedProperty<double> property, double value)
    {
        return new Style(x => x.Is<Control>().Class("p-4")) { Setters = { new Setter(property, value) } };
    }
}

/// <summary>
/// Minimal prototype of the composite-property host: four <see cref="double"/> side properties per composite
/// property (<see cref="double.NaN"/> = unset) plus a class handler that recomposes the real
/// <see cref="Thickness"/> value into a Style-priority value.
/// </summary>
internal static class CompositeGhostSpikeHost
{
    public static readonly AttachedProperty<double> MarginLeftProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("MarginLeft", typeof(CompositeGhostSpikeHost), double.NaN);

    public static readonly AttachedProperty<double> MarginTopProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("MarginTop", typeof(CompositeGhostSpikeHost), double.NaN);

    public static readonly AttachedProperty<double> MarginRightProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("MarginRight", typeof(CompositeGhostSpikeHost), double.NaN);

    public static readonly AttachedProperty<double> MarginBottomProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("MarginBottom", typeof(CompositeGhostSpikeHost), double.NaN);

    public static readonly AttachedProperty<double> PaddingLeftProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("PaddingLeft", typeof(CompositeGhostSpikeHost), double.NaN);

    public static readonly AttachedProperty<double> PaddingTopProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("PaddingTop", typeof(CompositeGhostSpikeHost), double.NaN);

    public static readonly AttachedProperty<double> PaddingRightProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("PaddingRight", typeof(CompositeGhostSpikeHost), double.NaN);

    public static readonly AttachedProperty<double> PaddingBottomProperty = AvaloniaProperty.RegisterAttached<
        Control,
        double
    >("PaddingBottom", typeof(CompositeGhostSpikeHost), double.NaN);

    // Declared after the properties on purpose: static field initializers run in textual order, so an array
    // declared above them would capture nulls.
    private static readonly AttachedProperty<double>[] s_marginSides =
    [
        MarginLeftProperty,
        MarginTopProperty,
        MarginRightProperty,
        MarginBottomProperty,
    ];

    private static readonly AttachedProperty<double>[] s_paddingSides =
    [
        PaddingLeftProperty,
        PaddingTopProperty,
        PaddingRightProperty,
        PaddingBottomProperty,
    ];

    /// <summary>
    /// Per-object, weakly held "our current contribution" slots, so a control that is collected does not keep the
    /// entry alive (and vice versa). A dedicated attached property would work too, but would itself be a value in the
    /// store — the table keeps the spike honest about the production design keeping bookkeeping out of the store.
    /// </summary>
    private static readonly ConditionalWeakTable<AvaloniaObject, Contributions> s_contributions = new();

    static CompositeGhostSpikeHost()
    {
        foreach (var property in s_marginSides)
            property.Changed.AddClassHandler<Control>((control, _) => RecomposeMargin(control));

        foreach (var property in s_paddingSides)
            property.Changed.AddClassHandler<Control>((control, _) => RecomposePadding(control));
    }

    /// <summary>Forces the static constructor (registration + class handlers) to run.</summary>
    /// <returns><c>true</c>, so a test can hold the result in a static field.</returns>
    public static bool EnsureInitialized() => true;

    private static void RecomposeMargin(Control control)
    {
        Apply(
            control,
            Layoutable.MarginProperty,
            control.GetValue(MarginLeftProperty),
            control.GetValue(MarginTopProperty),
            control.GetValue(MarginRightProperty),
            control.GetValue(MarginBottomProperty)
        );
    }

    private static void RecomposePadding(Control control)
    {
        // Padding is Decorator.PaddingProperty AddOwner'ed by TemplatedControl/TextBlock/ContentPresenter. A control
        // that never registered it (Canvas, Panel, ...) has nowhere for the value to be read from, so stay out of it.
        if (!AvaloniaPropertyRegistry.Instance.IsRegistered(control, Decorator.PaddingProperty))
            return;

        Apply(
            control,
            Decorator.PaddingProperty,
            control.GetValue(PaddingLeftProperty),
            control.GetValue(PaddingTopProperty),
            control.GetValue(PaddingRightProperty),
            control.GetValue(PaddingBottomProperty)
        );
    }

    private static void Apply(
        Control control,
        StyledProperty<Thickness> property,
        double left,
        double top,
        double right,
        double bottom
    )
    {
        var contribution = s_contributions.GetOrCreateValue(control).For(property);

        if (double.IsNaN(left) && double.IsNaN(top) && double.IsNaN(right) && double.IsNaN(bottom))
        {
            // All sides unset: drop our value so the property falls back to the theme/template/default value.
            contribution.Reset();
            return;
        }

        contribution.Set(
            control,
            property,
            new Thickness(ToPixels(left), ToPixels(top), ToPixels(right), ToPixels(bottom))
        );
    }

    private static double ToPixels(double side) => double.IsNaN(side) ? 0 : side;

    private sealed class Contributions
    {
        private Contribution? _margin;
        private Contribution? _padding;

        public Contribution For(StyledProperty<Thickness> property) =>
            property == Layoutable.MarginProperty ? _margin ??= new Contribution() : _padding ??= new Contribution();
    }

    /// <summary>
    /// One side-family contribution to one real property. <c>SetValue</c> with a non-local priority returns the
    /// <see cref="IDisposable"/> of the <c>ImmediateValueEntry</c> it inserted; that entry is immutable, so updating
    /// means inserting a new one and disposing the old. The order matters: set first, dispose second. Disposing first
    /// drops the effective value to the next priority (default/theme) for a moment, which is observable as a
    /// PropertyChanged with the fallback value between every recomposition. Set-then-dispose never drops: the new
    /// frame is appended after the old one, wins the equal-priority tie while both exist, and disposing the old entry
    /// then removes only a shadowed entry. Disposing it <em>is</em> what makes "all sides unset" fall back.
    /// </summary>
    private sealed class Contribution
    {
        private IDisposable? _entry;
        private Thickness? _current;

        public void Set(AvaloniaObject owner, StyledProperty<Thickness> property, Thickness value)
        {
            if (_current is { } current && current.Equals(value))
                return;

            var previous = _entry;
            _entry = owner.SetValue(property, value, BindingPriority.Style);
            _current = value;
            previous?.Dispose();
        }

        public void Reset()
        {
            _entry?.Dispose();
            _entry = null;
            _current = null;
        }
    }
}
