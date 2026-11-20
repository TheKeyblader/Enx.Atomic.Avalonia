using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Enx.Atomic.Avalonia.Composites;

/// <summary>
/// The shared runtime implementation behind every composite host: watches the four side attached properties
/// and recomposes the real composite property from them at <see cref="BindingPriority.Style"/>.
///
/// <para>
/// <b>Why Style, not StyleTrigger.</b> The side properties are written by class-selected utility styles, which
/// Avalonia frames at <c>StyleTrigger</c> (a class selector is an activator). The composed value has to be a
/// <em>normal</em> style value: it is a derived value, not a style of its own, and writing it at
/// <c>StyleTrigger</c> would make it outrank the very styles that feed it. At <c>Style</c> it loses to any
/// user style setting the real property (expected — an explicit style beats a composition of utilities) while
/// still beating <c>Theme</c>/default values.
/// </para>
///
/// <para>
/// <b>Why set-then-dispose.</b> <see cref="AvaloniaObject.SetValue{T}(StyledProperty{T}, T, BindingPriority)"/>
/// with a non-local priority returns the <see cref="IDisposable"/> of the value entry it inserted, and that
/// entry is immutable: an update inserts a new one, so the previous one must be disposed. Disposing first would
/// momentarily drop the effective value to the next priority (theme/default) and make that transient fallback
/// observable through <c>PropertyChanged</c>; inserting first lets the new, later frame win the equal-priority
/// tie while both exist, and disposing the old entry then removes only a shadowed one.
/// </para>
///
/// <para>
/// One instance exists per composite host (created in the host's static constructor), and it owns the
/// per-control bookkeeping for that one real property — a <see cref="ConditionalWeakTable{TKey,TValue}"/> so a
/// collected control does not keep its contribution (or the table) alive.
/// </para>
/// </summary>
internal sealed class CompositePropertyHandler<TValue>
{
    private readonly StyledProperty<TValue> _realProperty;
    private readonly AttachedProperty<double>[] _sides;
    private readonly Func<double, double, double, double, TValue> _compose;
    private readonly ConditionalWeakTable<AvaloniaObject, Contribution> _contributions = new();

    /// <summary>
    /// Creates a handler after checking the host's wiring against its own <paramref name="descriptor"/> — a host
    /// whose fields or registered names drifted from the descriptor would otherwise only misbehave at runtime,
    /// long after the mistake.
    /// </summary>
    /// <param name="descriptor">The host's descriptor; its real property and side names must match the other arguments.</param>
    /// <param name="realProperty">The typed composite property to write (same instance as <see cref="CompositePropertyDescriptor.RealProperty"/>).</param>
    /// <param name="sides">The four side attached properties, in the descriptor's slot order.</param>
    /// <param name="compose">Assembles the four pixel values into the real property's value.</param>
    /// <exception cref="ArgumentException">The arguments disagree with <paramref name="descriptor"/>.</exception>
    public CompositePropertyHandler(
        CompositePropertyDescriptor descriptor,
        StyledProperty<TValue> realProperty,
        AttachedProperty<double>[] sides,
        Func<double, double, double, double, TValue> compose
    )
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(realProperty);
        ArgumentNullException.ThrowIfNull(sides);
        ArgumentNullException.ThrowIfNull(compose);

        if (!ReferenceEquals(descriptor.RealProperty, realProperty))
            throw new ArgumentException(
                "The descriptor's real property is not the one this handler writes.",
                nameof(descriptor)
            );

        if (descriptor.AttachedPropertyNames.Count != sides.Length)
            throw new ArgumentException(
                "The descriptor and the side array disagree on the number of sides.",
                nameof(sides)
            );

        for (var index = 0; index < sides.Length; index++)
        {
            if (sides[index].Name != descriptor.AttachedPropertyNames[index])
                throw new ArgumentException(
                    $"Side {index} is '{sides[index].Name}' but the descriptor registers '{descriptor.AttachedPropertyNames[index]}'.",
                    nameof(sides)
                );
        }

        _realProperty = realProperty;
        _sides = sides;
        _compose = compose;
    }

    /// <summary>
    /// Subscribes to every side's <c>Changed</c> observable for <see cref="Control"/> — the only place a
    /// composite recomposition can be triggered from. Called once, from the host's static constructor.
    /// </summary>
    public void RegisterClassHandlers()
    {
        foreach (var side in _sides)
            side.Changed.AddClassHandler<Control>((control, _) => Recompose(control));
    }

    /// <summary>
    /// Recomputes the real property from the control's four side values and writes it (or withdraws the
    /// contribution when every side is unset). Called from the class handlers registered by
    /// <see cref="RegisterClassHandlers"/>.
    /// </summary>
    private void Recompose(Control control)
    {
        // Padding is Decorator.PaddingProperty AddOwner'ed by TemplatedControl/TextBlock/ContentPresenter;
        // Canvas and friends never register it. Setting a property nobody reads would be silent but pointless,
        // and the registry check is exactly the cheap guard that keeps the handler honest.
        if (!AvaloniaPropertyRegistry.Instance.IsRegistered(control, _realProperty))
            return;

        var contribution = _contributions.GetOrCreateValue(control);
        var first = control.GetValue(_sides[0]);
        var second = control.GetValue(_sides[1]);
        var third = control.GetValue(_sides[2]);
        var fourth = control.GetValue(_sides[3]);

        if (double.IsNaN(first) && double.IsNaN(second) && double.IsNaN(third) && double.IsNaN(fourth))
        {
            // Every side unset: drop our value so the property falls back to the theme/template/default value.
            contribution.Reset();
            return;
        }

        contribution.Set(
            control,
            _realProperty,
            _compose(ToPixels(first), ToPixels(second), ToPixels(third), ToPixels(fourth))
        );
    }

    /// <summary>An unset side contributes <c>0</c> to the composed struct.</summary>
    private static double ToPixels(double side) => double.IsNaN(side) ? 0 : side;

    /// <summary>
    /// One control's current contribution to one real property. The value entry is immutable (see the type's
    /// remarks), so <see cref="Set"/> inserts the new one before disposing the previous; <see cref="_current"/>
    /// skips a write when a side changes but the composed value doesn't.
    /// </summary>
    private sealed class Contribution
    {
        private IDisposable? _entry;
        private TValue _current = default!;
        private bool _hasCurrent;

        public void Set(AvaloniaObject owner, StyledProperty<TValue> property, TValue value)
        {
            if (_hasCurrent && EqualityComparer<TValue>.Default.Equals(_current, value))
                return;

            var previous = _entry;
            _entry = owner.SetValue(property, value, BindingPriority.Style);
            _current = value;
            _hasCurrent = true;
            previous?.Dispose();
        }

        public void Reset()
        {
            _entry?.Dispose();
            _entry = null;
            _current = default!;
            _hasCurrent = false;
        }
    }
}
