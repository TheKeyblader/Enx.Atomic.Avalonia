using System.Globalization;

namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>Emits booleans, numeric primitives and strings.</summary>
public sealed class PrimitiveValueEmitter : IValueEmitter
{
    private static readonly HashSet<Type> Supported =
    [
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(string),
    ];

    /// <inheritdoc/>
    public bool CanHandle(Type type) => Supported.Contains(type);

    /// <inheritdoc/>
    public IEnumerable<string> GetNamespaces(object value) => [];

    /// <inheritdoc/>
    public string Emit(object value) =>
        value switch
        {
            bool b => b ? "true" : "false",
            string s => CSharpLiteral.String(s),
            float f => CSharpLiteral.Float(f),
            double d => CSharpLiteral.Double(d),
            decimal m => $"{m.ToString(CultureInfo.InvariantCulture)}m",
            long l => $"{l}L",
            ulong ul => $"{ul}UL",
            uint ui => $"{ui}U",
            byte or sbyte or short or ushort or int => Convert.ToString(value, CultureInfo.InvariantCulture)!,
            _ => throw new NotSupportedException($"'{value.GetType()}' isn't a supported primitive."),
        };
}
