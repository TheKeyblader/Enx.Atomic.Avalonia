using Avalonia.Media;
using Enx.Atomic.Avalonia.CodeGen;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests.Snapshots;

/// <summary>
/// Snapshot coverage for the two codegen outputs: a representative token set covering every rule family and
/// variant emitted through <see cref="StyleEmitter.Emit"/>, and its resource dictionary through
/// <see cref="ResourceDictionaryEmitter.Emit"/>. These complement — they don't replace — <c>CodeGenTests</c>:
/// the Roslyn compile check proves the emitted text is valid C#, while the snapshot makes every byte change
/// reviewable in a diff.
///
/// <para>
/// The source is fixed, and each test also asserts the emitter is byte-for-byte repeatable, so an ordering or
/// dictionary-iteration regression fails here instead of being hidden by re-accepting the snapshot.
/// </para>
/// </summary>
public class EmittedCodeSnapshotTests
{
    /// <summary>
    /// Every family and variant in one token set: static utilities, theme colors, an explicitly themed color
    /// (both Light and Dark dictionaries), arbitrary values, grid/structure, size, spacing, per-side border and
    /// radius utilities (including the rank-ordered uniform/axis/side cases), and the pseudo-class, breakpoint
    /// and dark variants chained together.
    /// </summary>
    private const string Source = """
        hidden visible overflow-hidden pointer-events-none enabled not-focusable ltr
        flex-row flex-col justify-self-center self-end items-center justify-items-start
        scroll-x-auto object-cover italic bold text-center underline no-underline truncate cursor-pointer
        bg-red-500 text-blue-600 border-green-700 bg-brand-500
        bg-[#ff0000] text-[18px] border-[3px] m-[10px] w-[123px] rounded-[6px]
        w-4 h-8 min-w-0 max-w-xl gap-2 gap-x-1
        m-1 mx-2 ml-4 -mt-2 p-1 px-2 pt-3
        border border-2 border-t-4 rounded-md rounded-tl-lg
        shadow-md shadow-inner
        grid-cols-3 grid-rows-2 col-span-2 row-span-2
        size-4 border-x-2 opacity-50 z-10 leading-[20px] tracking-[1px] font-serif font-sans top-4
        hover:w-8 w-4 sm:flex-col max-sm:w-4 dark:bg-slate-800 dark:hover:bg-red-600
        """;

    [AvaloniaFact]
    public async Task Styles()
    {
        var (configuration, generator) = CreateConfiguredGenerator();
        var utils = generator.Generate(Source, new AtomicGenerator<MiniTheme>.Options());

        var styles = StyleEmitter.Emit(utils, "Enx.Atomic.Avalonia.Tests.Snapshots", "AtomicStyles", "top-level");

        Assert.Equal(styles, EmitStyles(generator));
        await Verify(styles, "cs");
    }

    [AvaloniaFact]
    public async Task Resources()
    {
        var (configuration, generator) = CreateConfiguredGenerator();
        _ = generator.Generate(Source, new AtomicGenerator<MiniTheme>.Options());

        var resources = ResourceDictionaryEmitter.Emit(
            generator.ResolvedResources,
            configuration.Theme,
            "Enx.Atomic.Avalonia.Tests.Snapshots",
            "AtomicResources"
        );

        Assert.Equal(resources, EmitResources(configuration, generator));
        await Verify(resources, "cs");
    }

    private static string EmitStyles(AtomicGenerator<MiniTheme> generator)
    {
        var utils = generator.Generate(Source, new AtomicGenerator<MiniTheme>.Options());
        return StyleEmitter.Emit(utils, "Enx.Atomic.Avalonia.Tests.Snapshots", "AtomicStyles", "top-level");
    }

    private static string EmitResources(
        AtomicConfiguration<MiniTheme> configuration,
        AtomicGenerator<MiniTheme> generator
    ) =>
        ResourceDictionaryEmitter.Emit(
            generator.ResolvedResources,
            configuration.Theme,
            "Enx.Atomic.Avalonia.Tests.Snapshots",
            "AtomicResources"
        );

    /// <summary>
    /// A fresh Mini configuration with one explicitly themed color (<c>brand-500</c>) added, so
    /// <see cref="ResourceDictionaryEmitter"/>'s Light/Dark split is part of the snapshot rather than only
    /// covered by its unit tests.
    /// </summary>
    private static (
        AtomicConfiguration<MiniTheme> Configuration,
        AtomicGenerator<MiniTheme> Generator
    ) CreateConfiguredGenerator()
    {
        var (configuration, generator) = TestHelpers.CreateMiniGenerator();
        configuration.Theme.Colors["brand-500"] = new Themed<IBrush>(
            new SolidColorBrush(Color.Parse("#ff112233")),
            new SolidColorBrush(Color.Parse("#ffddeeff"))
        );
        return (configuration, generator);
    }
}
