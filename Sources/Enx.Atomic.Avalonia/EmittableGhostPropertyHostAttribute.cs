namespace Enx.Atomic.Avalonia;

/// <summary>
/// Marks a class declaring one or more "ghost" <see cref="AvaloniaProperty"/>s that can't be resolved away
/// into a pre-existing Avalonia property before emission, unlike <see cref="SpecialProperties"/>/<see cref="GhostPropertyCombiner{TTheme}"/>
/// — the class itself has to exist as real compiled code wherever a <see cref="Avalonia.Styling.Setter"/>
/// targeting one of its properties is applied (e.g. Preset.Mini's <c>GridDefinitions</c>, whose class handler
/// is the only thing standing in for a property Avalonia itself doesn't expose).
///
/// For the runtime resolution path that's automatic — the consumer already references whatever assembly
/// declares it, to call e.g. <c>AddMiniTheme</c>. For the build-time codegen path it isn't: the consuming
/// project's own project chain may not reference that assembly at all (see the "Build-time C# code
/// generation" section of <c>ARCHITECTURE.md</c>) — <c>Enx.Atomic.Avalonia.CodeGen</c> looks for this
/// attribute via reflection (never taking a compile-time dependency on the preset that defines it) and, when
/// found, embeds the marked class's own <c>EmbeddableSource</c> once into the generated file instead of
/// referencing the original type.
///
/// The marked class must expose a <c>public const string EmbeddableSource</c> holding a complete,
/// self-contained C# class declaration — no namespace, and every type referenced via a <c>global::</c>-qualified
/// name, so the embedded copy needs no <c>using</c> directives contributed by the rest of the file. Its own
/// simple name (as text) must exactly match the marked class's <see cref="Type.Name"/>, and its property
/// fields must be named exactly like the marked class's own fields — the emitter reuses the original type's
/// field-name lookup (see <c>AvaloniaPropertyNaming</c>) and just swaps which type name prefixes it.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EmittableGhostPropertyHostAttribute : Attribute;
