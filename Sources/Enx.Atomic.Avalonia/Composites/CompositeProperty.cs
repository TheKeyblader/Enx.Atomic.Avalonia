using Avalonia;

namespace Enx.Atomic.Avalonia.Composites;

/// <summary>
/// The public entry point for wiring a composite host — the missing half of a user-defined
/// <see cref="CompositePropertyDescriptor"/>: the descriptor tells the code generator what to embed, and this
/// method registers the runtime recomposition behind those four sides.
///
/// <para>
/// A host is a static class declaring four public <c>AttachedProperty&lt;double&gt;</c> fields in
/// <see cref="CompositePropertyDescriptor.Kind"/>'s slot order and a descriptor; its static constructor then calls
/// <see cref="Register"/>. Because <see cref="Register"/> only knows the descriptor and the sides, the compose
/// function is picked from <see cref="CompositePropertyDescriptor.Kind"/> rather than supplied by the caller —
/// a host can't accidentally pair a <see cref="Avalonia.Thickness"/> descriptor with a
/// <see cref="Avalonia.CornerRadius"/> constructor, and the real property's CLR type is checked against the kind.
/// </para>
/// <para>
/// Call it from the host's explicit static constructor (as the shipped hosts do): that makes the type not
/// <c>beforefieldinit</c>, so the handlers are registered before the first side property read.
/// </para>
/// </summary>
public static class CompositeProperty
{
    /// <summary>
    /// Registers the class handlers that recompose <paramref name="descriptor"/>'s real property from
    /// <paramref name="sides"/> whenever one of them changes.
    /// </summary>
    /// <param name="descriptor">The host's descriptor; it names the real property, the kind and the four sides.</param>
    /// <param name="sides">The four side attached properties, in the descriptor's slot order.</param>
    /// <exception cref="ArgumentException">
    /// The descriptor's <see cref="CompositePropertyDescriptor.Kind"/> doesn't match its real property's CLR type,
    /// or the sides don't match the descriptor's names (validated by the internal handler).
    /// </exception>
    public static void Register(CompositePropertyDescriptor descriptor, params AttachedProperty<double>[] sides)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        switch (descriptor.Kind)
        {
            case CompositeKind.Thickness:
                if (descriptor.RealProperty is not StyledProperty<Thickness> thickness)
                    throw Mismatch(descriptor, typeof(Thickness));

                new CompositePropertyHandler<Thickness>(
                    descriptor,
                    thickness,
                    sides,
                    static (left, top, right, bottom) => new Thickness(left, top, right, bottom)
                ).RegisterClassHandlers();
                break;

            case CompositeKind.CornerRadius:
                if (descriptor.RealProperty is not StyledProperty<CornerRadius> cornerRadius)
                    throw Mismatch(descriptor, typeof(CornerRadius));

                new CompositePropertyHandler<CornerRadius>(
                    descriptor,
                    cornerRadius,
                    sides,
                    static (topLeft, topRight, bottomRight, bottomLeft) =>
                        new CornerRadius(topLeft, topRight, bottomRight, bottomLeft)
                ).RegisterClassHandlers();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.Kind, "Unknown composite kind.");
        }
    }

    /// <summary>
    /// Builds the error for a descriptor whose real property isn't the struct its kind composes — a mistake that
    /// would otherwise surface as an <see cref="InvalidCastException"/> the first time a side changes.
    /// </summary>
    private static ArgumentException Mismatch(CompositePropertyDescriptor descriptor, Type expected) =>
        new(
            $"Composite kind {descriptor.Kind} requires a StyledProperty<{expected.Name}> as its real property, "
                + $"but '{descriptor.RealProperty.Name}' is a {descriptor.RealProperty.GetType().Name}.",
            nameof(descriptor)
        );
}
