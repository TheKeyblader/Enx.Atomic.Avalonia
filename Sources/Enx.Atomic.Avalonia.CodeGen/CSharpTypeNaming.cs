namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>Converts a runtime <see cref="Type"/> into the C# type name text an emitted source file would reference it by.</summary>
internal static class CSharpTypeNaming
{
    private static readonly Dictionary<Type, string> BuiltIns = new()
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(char)] = "char",
        [typeof(string)] = "string",
        [typeof(object)] = "object",
    };

    /// <summary>The unqualified type name (e.g. <c>"Button"</c>, <c>"List&lt;string&gt;"</c>) — namespaces are handled separately via <c>using</c> directives.</summary>
    public static string GetName(Type type)
    {
        if (BuiltIns.TryGetValue(type, out var builtIn))
            return builtIn;

        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return $"{GetName(underlying)}?";

        if (!type.IsGenericType)
            return type.Name;

        var name = type.Name[..type.Name.IndexOf('`')];
        var args = string.Join(", ", type.GetGenericArguments().Select(GetName));
        return $"{name}<{args}>";
    }

    /// <summary>Every namespace referenced by <paramref name="type"/> and its generic arguments, for collecting <c>using</c> directives.</summary>
    public static IEnumerable<string> GetNamespaces(Type type)
    {
        if (type.Namespace is not null)
            yield return type.Namespace;

        if (!type.IsGenericType)
            yield break;

        foreach (var arg in type.GetGenericArguments())
        foreach (var ns in GetNamespaces(arg))
            yield return ns;
    }
}
