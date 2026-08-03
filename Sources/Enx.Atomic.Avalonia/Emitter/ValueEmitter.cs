namespace Enx.Atomic.Avalonia;

/// <summary>
/// Converts a resolved style value into C# source, for code-generating emitters such as <see cref="Enx.Atomic.Avalonia.CSharp.CSharpEmitter{TTheme}"/>.
/// Registered on <see cref="AtomicConfiguration{TTheme}.Emitters"/> and looked up by value type via <see cref="CanHandle(Type)"/>.
/// </summary>
public abstract class ValueEmitter
{
    /// <summary>Whether this emitter knows how to convert values of <paramref name="type"/>.</summary>
    public abstract bool CanHandle(Type type);

    /// <summary>The namespaces that must be imported for the code produced by <see cref="ToCSharpString(object, out string?)"/> to compile.</summary>
    public abstract IEnumerable<string> GetUsings();

    /// <summary>
    /// Converts <paramref name="value"/> to a C# expression. If the value needs a helper local variable
    /// (e.g. because it can't be expressed inline), returns the full declaration statement and sets
    /// <paramref name="varName"/> to the variable's name; otherwise <paramref name="varName"/> is <see langword="null"/>
    /// and the return value is the inline expression itself.
    /// </summary>
    public abstract string ToCSharpString(object value, out string? varName);
}

/// <summary>Strongly-typed base for a <see cref="ValueEmitter"/> that handles exactly one value type.</summary>
public abstract class ValueEmitter<TValue> : ValueEmitter
{
    /// <inheritdoc/>
    public override bool CanHandle(Type type) => type == typeof(TValue);

    /// <inheritdoc/>
    public override string ToCSharpString(object value, out string? varName) => ToCSharpString((TValue)value, out varName);

    /// <summary>Converts <paramref name="value"/> to a C# expression; see <see cref="ValueEmitter.ToCSharpString(object, out string?)"/> for the <paramref name="varName"/> contract.</summary>
    public abstract string ToCSharpString(TValue value, out string? varName);
}
