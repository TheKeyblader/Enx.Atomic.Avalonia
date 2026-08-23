namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>Emits a runtime value (a <c>Setter</c>'s value, a selector's <c>PropertyEquals</c> value, ...) as C# literal text.</summary>
public interface IValueEmitter
{
    /// <summary>Whether this emitter knows how to emit a non-null value of <paramref name="type"/>.</summary>
    bool CanHandle(Type type);

    /// <summary>Namespaces the emitted text for <paramref name="value"/> depends on.</summary>
    IEnumerable<string> GetNamespaces(object value);

    /// <summary>The C# expression text for <paramref name="value"/> (never called with <see langword="null"/> — that's handled by the caller).</summary>
    string Emit(object value);
}
