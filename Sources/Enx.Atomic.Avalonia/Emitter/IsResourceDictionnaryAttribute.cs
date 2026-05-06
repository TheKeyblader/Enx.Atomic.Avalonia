namespace Enx.Atomic.Avalonia;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public class IsResourceDictionnaryAttribute : Attribute
{
    public virtual bool ShouldInsert(string name)
        => true;
}
