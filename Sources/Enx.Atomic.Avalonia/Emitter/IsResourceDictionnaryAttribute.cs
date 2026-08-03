namespace Enx.Atomic.Avalonia;

/// <summary>
/// Marks a field or property on a theme type as containing resource entries that should be surfaced as
/// generated resource keys. Read by <see cref="EmitterHelpers.GetThemeKeys(object, List{ValueEmitter}, string)"/>
/// when walking the theme instance to discover resource names.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public class IsResourceDictionnaryAttribute : Attribute
{
    /// <summary>Whether the resource entry named <paramref name="name"/> should be included. Defaults to always <see langword="true"/>; override to filter.</summary>
    public virtual bool ShouldInsert(string name)
        => true;
}
