using Enx.Atomic.Avalonia.Composites;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// <see cref="StyleOrdering.Distinct"/> is what collapses a transformer's per-file <c>AddUtil</c> output in a
/// multi-source build (see <c>AtomicCliTests</c>). It has to compare data, not instances — two runs of the same
/// rule over the same token are different objects — without merging styles it cannot prove identical.
/// </summary>
public class StyleOrderingTests
{
    [AvaloniaFact]
    public void IdenticalStyles_FromSeparateResolutions_CollapseToOne()
    {
        // Separate generators: each ParseToken builds its own selector expression and boxed values, which is
        // exactly the shape a transformer that runs once per source file produces.
        var first = TestHelpers.CreateMiniGenerator().Generator.ParseToken("p-4");
        var second = TestHelpers.CreateMiniGenerator().Generator.ParseToken("p-4");

        var distinct = StyleOrdering.Distinct([.. first, .. second]);

        Assert.Single(distinct);
    }

    [AvaloniaFact]
    public void StylesDifferingOnlyByValue_AreBothKept()
    {
        var first = TestHelpers.CreateMiniGenerator().Generator.ParseToken("p-4");
        var second = TestHelpers.CreateMiniGenerator().Generator.ParseToken("p-5");

        Assert.Equal(2, StyleOrdering.Distinct([.. first, .. second]).Length);
    }

    [AvaloniaFact]
    public void StylesDifferingOnlyByClass_AreBothKept()
    {
        var left = CompositeTestSource.StyleUtil("left", 0, (PaddingSides.PaddingLeftProperty, 16d));
        var right = CompositeTestSource.StyleUtil("right", 0, (PaddingSides.PaddingLeftProperty, 16d));

        Assert.Equal(2, StyleOrdering.Distinct([left, right]).Length);
    }
}
