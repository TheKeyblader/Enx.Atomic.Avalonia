using FastExpressionCompiler;

namespace Enx.Atomic.Avalonia;

/// <summary>Emits C# literals for primitive .NET types (bool, numeric types, string) using <c>ExpressionToCodeLib</c>.</summary>
public class BasicTypeEmitter : ValueEmitter
{
    /// <summary>The types this emitter can convert to a C# literal.</summary>
    public static readonly Type[] SupportTypes =
    [
        typeof(bool),
        typeof(string),
        typeof(byte),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(ushort),
        typeof(uint),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
    ];

    /// <inheritdoc/>
    public override bool CanHandle(Type type) => SupportTypes.Contains(type);

    /// <inheritdoc/>
    public override IEnumerable<string> GetUsings()
    {
        yield return "System";
    }

    /// <inheritdoc/>
    public override string ToCSharpString(object value, out string? varName)
    {
        varName = null;
        return value.ToCode();
    }
}
