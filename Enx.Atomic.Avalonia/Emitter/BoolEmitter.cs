using FastExpressionCompiler;

namespace Enx.Atomic.Avalonia;

public class BasicTypeEmitter : ValueEmitter
{
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

    public override bool CanHandle(Type type) => SupportTypes.Contains(type);

    public override IEnumerable<string> GetUsings()
    {
        yield return "System";
    }

    public override string ToCSharpString(object value, out string? varName)
    {
        varName = null;
        return value.ToCode();
    }
}
