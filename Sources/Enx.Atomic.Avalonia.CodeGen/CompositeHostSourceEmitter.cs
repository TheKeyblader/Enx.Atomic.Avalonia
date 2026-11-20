using System.Text;
using Enx.Atomic.Avalonia.Composites;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>
/// Turns a <see cref="CompositePropertyDescriptor"/> into the self-contained C# source of a host class — four
/// <c>AttachedProperty&lt;double&gt;</c> fields plus the class handlers that recompose the real composite
/// property. The generated copy is what runs in a project that only consumes the build-time output (it may not
/// reference the core engine or the preset assembly at all), so everything it needs is emitted once alongside
/// it as <see cref="HelperSource"/>.
///
/// <para>
/// The runtime hosts (<c>MarginSides</c> and friends) implement the same behavior in real C#; the two cannot
/// be generated from one another without either embedding source in the runtime assembly or reflecting over
/// it, so they are kept in step by an identical-behavior test suite that runs every scenario against both
/// copies (see the tests). This class deliberately does not read the runtime host's implementation.
/// </para>
/// </summary>
internal static class CompositeHostSourceEmitter
{
    /// <summary>
    /// The compose/set-then-dispose machinery shared by every embedded composite host — emitted once per
    /// generated file, before the hosts that use it. <c>file</c>-scoped so a generated file can never collide
    /// with a real type of the same name in the consuming project.
    /// </summary>
    public const string HelperSource = """
        file static class CompositeSideRecomposition
        {
            internal sealed class Family<TValue>
            {
                private readonly global::Avalonia.StyledProperty<TValue> _property;
                private readonly global::Avalonia.AttachedProperty<double>[] _sides;
                private readonly global::System.Func<double, double, double, double, TValue> _compose;
                private readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<global::Avalonia.AvaloniaObject, Contribution> _contributions = new();

                public Family(
                    global::Avalonia.StyledProperty<TValue> property,
                    global::Avalonia.AttachedProperty<double>[] sides,
                    global::System.Func<double, double, double, double, TValue> compose
                )
                {
                    _property = property;
                    _sides = sides;
                    _compose = compose;
                }

                public void RegisterClassHandlers()
                {
                    foreach (var side in _sides)
                    {
                        global::Avalonia.AvaloniaObjectExtensions.AddClassHandler<global::Avalonia.Controls.Control>(
                            side.Changed,
                            (control, _) => Recompose(control)
                        );
                    }
                }

                private void Recompose(global::Avalonia.Controls.Control control)
                {
                    if (!global::Avalonia.AvaloniaPropertyRegistry.Instance.IsRegistered(control, _property))
                        return;

                    var contribution = _contributions.GetOrCreateValue(control);
                    var first = control.GetValue(_sides[0]);
                    var second = control.GetValue(_sides[1]);
                    var third = control.GetValue(_sides[2]);
                    var fourth = control.GetValue(_sides[3]);

                    if (double.IsNaN(first) && double.IsNaN(second) && double.IsNaN(third) && double.IsNaN(fourth))
                    {
                        contribution.Reset();
                        return;
                    }

                    contribution.Set(
                        control,
                        _property,
                        _compose(ToPixels(first), ToPixels(second), ToPixels(third), ToPixels(fourth))
                    );
                }

                private static double ToPixels(double side) => double.IsNaN(side) ? 0d : side;

                private sealed class Contribution
                {
                    private global::System.IDisposable? _entry;
                    private TValue _current = default!;
                    private bool _hasCurrent;

                    public void Set(
                        global::Avalonia.AvaloniaObject owner,
                        global::Avalonia.StyledProperty<TValue> property,
                        TValue value
                    )
                    {
                        if (
                            _hasCurrent
                            && global::System.Collections.Generic.EqualityComparer<TValue>.Default.Equals(_current, value)
                        )
                            return;

                        var previous = _entry;
                        _entry = owner.SetValue(property, value, global::Avalonia.Data.BindingPriority.Style);
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
        }
        """;

    /// <summary>Emits the <c>file</c>-local host class for <paramref name="descriptor"/>, with the same field and registered names and the same behavior as its runtime counterpart.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="descriptor"/> uses a <see cref="CompositeKind"/> this emitter doesn't know.</exception>
    public static string EmitHost(CompositePropertyDescriptor descriptor)
    {
        var host = descriptor.HostTypeName;
        var realProperty = GlobalPropertyExpression(descriptor);
        var (compositeType, parameters) = descriptor.Kind switch
        {
            CompositeKind.Thickness => ("global::Avalonia.Thickness", "left, top, right, bottom"),
            CompositeKind.CornerRadius => (
                "global::Avalonia.CornerRadius",
                "topLeft, topRight, bottomRight, bottomLeft"
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.Kind,
                "No composite struct is known for this kind."
            ),
        };

        var source = new StringBuilder();
        source.AppendLine($"file static class {host}");
        source.AppendLine("{");
        for (var index = 0; index < 4; index++)
        {
            source.AppendLine(
                $"    public static readonly global::Avalonia.AttachedProperty<double> {descriptor.SideFields[index]} ="
            );
            source.AppendLine(
                "        global::Avalonia.AvaloniaProperty.RegisterAttached<global::Avalonia.Controls.Control, double>("
            );
            source.AppendLine($"            {CSharpLiteral.String(descriptor.AttachedPropertyNames[index])},");
            source.AppendLine($"            typeof({host}),");
            source.AppendLine("            double.NaN");
            source.AppendLine("        );");
            source.AppendLine();
        }

        source.AppendLine($"    static {host}()");
        source.AppendLine("    {");
        source.AppendLine($"        new CompositeSideRecomposition.Family<{compositeType}>(");
        source.AppendLine($"            {realProperty},");
        source.AppendLine("            new global::Avalonia.AttachedProperty<double>[]");
        source.AppendLine("            {");
        foreach (var field in descriptor.SideFields)
            source.AppendLine($"                {field},");
        source.AppendLine("            },");
        source.AppendLine($"            static ({parameters}) => new {compositeType}({parameters})");
        source.AppendLine("        ).RegisterClassHandlers();");
        source.AppendLine("    }");
        source.Append("}");
        return source.ToString();
    }

    /// <summary>
    /// The real property as an expression that needs no <c>using</c> directive, e.g.
    /// <c>global::Avalonia.Layout.Layoutable.MarginProperty</c>. <see cref="AvaloniaProperty.OwnerType"/> is
    /// where the public static field holding the instance lives — for an <c>AddOwner</c>'ed property that is
    /// the original registrant, which is exactly the field <c>AvaloniaPropertyNaming</c> returns.
    /// </summary>
    private static string GlobalPropertyExpression(CompositePropertyDescriptor descriptor)
    {
        var owner = descriptor.RealProperty.OwnerType;
        var field = AvaloniaPropertyNaming.GetFieldName(descriptor.RealProperty);
        return $"global::{owner.FullName?.Replace('+', '.')}.{field}";
    }
}
