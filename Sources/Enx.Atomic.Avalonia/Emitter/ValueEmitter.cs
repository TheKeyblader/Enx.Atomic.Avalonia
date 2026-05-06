namespace Enx.Atomic.Avalonia;

public abstract class ValueEmitter
{
    public abstract bool CanHandle(Type type);

    public abstract IEnumerable<string> GetUsings();

    public abstract string ToCSharpString(object value, out string? varName);
}

public abstract class ValueEmitter<TValue> : ValueEmitter
{
    public override bool CanHandle(Type type) => type == typeof(TValue);

    public override string ToCSharpString(object value, out string? varName) => ToCSharpString((TValue)value, out varName);

    public abstract string ToCSharpString(TValue value, out string? varName);
}
