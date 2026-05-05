namespace Enx.Atomic.Avalonia;

public abstract class ValueEmitter
{
    public abstract bool CanHandle(Type type);

    public abstract IEnumerable<string> GetUsings();
}

public abstract class ValueEmitter<TValue> : ValueEmitter
{
    public override bool CanHandle(Type type)
        => type == typeof(TValue);
}
