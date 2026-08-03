using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Styling;

namespace Enx.Atomic.Avalonia;

/// <summary>Shared helpers for code-generating <see cref="IStyleEmitter{TTheme}"/> implementations.</summary>
public static class EmitterHelpers
{
    /// <summary>
    /// Collects every namespace that must be imported for generated code referencing <paramref name="utils"/>
    /// to compile: each style's property owner type namespace plus whatever <see cref="ValueEmitter.GetUsings"/>
    /// reports for its value type.
    /// </summary>
    /// <exception cref="InvalidOperationException">A style's value type has no matching emitter in <paramref name="emitters"/>.</exception>
    public static HashSet<string> GetUsings(StringifiedUtil[] utils, List<ValueEmitter> emitters)
    {
        HashSet<string> usings = [];

        var propertyTypes = utils
            .SelectMany(u => u.Body)
            .Select(p => new { p.Property!.OwnerType, ValueType = p.Property!.PropertyType })
            .Distinct();

        foreach (var propertyType in propertyTypes)
        {
            if (propertyType.OwnerType.Namespace is not null)
                usings.Add(propertyType.OwnerType.Namespace);

            var emitter =
                emitters.FirstOrDefault(x => x.CanHandle(propertyType.ValueType))
                ?? throw new InvalidOperationException(
                    $"No emitter for type {propertyType.ValueType}"
                );

            foreach (var @using in emitter.GetUsings())
                usings.Add(@using);
        }

        return usings;
    }

    /// <summary>The kind of type a <see cref="Type"/> reflects, as guessed by <see cref="GuessTypeClass(Type)"/> from its metadata shape.</summary>
    public enum TypeClass
    {
        /// <summary>A primitive, enum, string, decimal, or <see cref="Type"/>-assignable type, or an array.</summary>
        BuiltinType,

        /// <summary>A compiler-generated anonymous type (e.g. from <c>new { ... }</c>).</summary>
        AnonymousType,

        /// <summary>A compiler-generated closure (display class) capturing local variables.</summary>
        ClosureType,

        /// <summary>A user-defined, non-builtin value type.</summary>
        StructType,

        /// <summary>An ordinary, non-compiler-generated reference type.</summary>
        NormalType,

        /// <summary>A closure generated for a top-level statements program's entry point.</summary>
        TopLevelProgramClosureType,
    }

    /// <summary>Heuristically classifies <paramref name="type"/> by inspecting its name and metadata, since the CLR does not expose this distinction directly.</summary>
    /// <exception cref="ArgumentException"><paramref name="type"/> doesn't match any recognized shape.</exception>
    public static TypeClass GuessTypeClass(this Type type)
    {
        var typeInfo = type.GetTypeInfo();
        if (typeInfo.IsArray)
        {
            return TypeClass.BuiltinType;
        }

        var compilerGenerated = typeInfo
            .GetCustomAttributes(typeof(CompilerGeneratedAttribute), false)
            .Any();
        var name = type.Name;
        var named_DisplayClass = name.Contains("_DisplayClass");
        var name_StartWithLessThan = name.StartsWith('<');
        var isBuiltin =
            typeInfo.IsPrimitive
            || typeInfo.IsEnum
            || type == typeof(decimal)
            || type == typeof(string)
            || typeof(Type).GetTypeInfo().IsAssignableFrom(type);

        if (name_StartWithLessThan && compilerGenerated)
        {
            var named_AnonymousType = name.Contains("AnonymousType");
            var isGeneric = typeInfo.IsGenericType;
            var isNested = type.IsNested;

            return isBuiltin switch
            {
                false when isGeneric && !isNested && named_AnonymousType => TypeClass.AnonymousType,
                false when isNested && named_DisplayClass => TypeClass.ClosureType,
                _ => throw new ArgumentException(
                    $"Can't deal with unknown-style compiler generated class {type.FullName} {named_AnonymousType}, {named_DisplayClass}, {isGeneric}, {isNested}"
                ),
            };
        }
        else
        {
            return compilerGenerated switch
            {
                false when !name_StartWithLessThan => isBuiltin ? TypeClass.BuiltinType
                : typeInfo.IsValueType ? TypeClass.StructType
                : TypeClass.NormalType,
                false
                    when (
                        name_StartWithLessThan
                        && named_DisplayClass
                        && type.Namespace is null
                        && type.IsNested
                        && type.DeclaringType == type.Assembly.EntryPoint?.DeclaringType
                    ) => TypeClass.TopLevelProgramClosureType,
                _ => throw new ArgumentException($"Unusual type, heuristics uncertain:{name}"),
            };
        }
    }

    /// <summary>Suffix stripped from a theme property name to derive its base resource key (e.g. <c>BackgroundDark</c> → <c>Background</c>).</summary>
    public const string DarkEndName = "Dark";

    /// <summary>
    /// Recursively walks <paramref name="theme"/>'s properties marked with <see cref="IsResourceDictionnaryAttribute"/>
    /// to discover the full set of resource keys it exposes: dictionary-valued properties contribute one key
    /// per dictionary entry, emitter-handleable properties contribute their own name, and everything else is
    /// walked recursively with its name appended to <paramref name="prefix"/>.
    /// </summary>
    /// <param name="theme">The theme instance (or nested value) to walk.</param>
    /// <param name="emitters">Emitters used to decide whether a property's type is a leaf resource value.</param>
    /// <param name="prefix">Key prefix accumulated from enclosing properties during recursion.</param>
    public static HashSet<string> GetThemeKeys(
        object theme,
        List<ValueEmitter> emitters,
        string prefix = ""
    )
    {
        var dictKeys = new HashSet<string>();

        var properties = theme
            .GetType()
            .GetProperties()
            .Where(p => p.GetCustomAttribute<IsResourceDictionnaryAttribute>() != null);

        foreach (var property in properties)
        {
            var propName = property.Name;
            if (propName.EndsWith(DarkEndName))
                propName = propName[..^DarkEndName.Length];

            var propertyValue = property.GetValue(theme)!;
            if (IsAssignableToDictionary(property.PropertyType))
            {
                var propertyKeys =
                    property.PropertyType.GetProperty("Keys")
                    ?? throw new InvalidOperationException();
                var keys = (ICollection<string>)propertyKeys.GetValue(propertyValue)!;
                foreach (var key in keys)
                    dictKeys.Add($"{prefix}{propName}{key}");

                continue;
            }

            var emitter = emitters.FirstOrDefault(x => x.CanHandle(property.PropertyType));
            if (emitter is not null)
            {
                dictKeys.Add($"{prefix}{propName}");
                continue;
            }

            var nestedKeys = GetThemeKeys(propertyValue, emitters, $"{prefix}{propName}");
            foreach (var key in nestedKeys)
                dictKeys.Add(key);
        }

        return dictKeys;
    }

    /// <summary>Whether <paramref name="type"/> or one of its interfaces is an <see cref="IDictionary{TKey, TValue}"/> keyed by <see cref="string"/>.</summary>
    public static bool IsAssignableToDictionary(Type type)
    {
        return type.GetInterfaces()
            .Concat([type])
            .Any(t =>
                t.IsConstructedGenericType
                && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                && t.GetGenericArguments()[0] == typeof(string)
            );
    }
}
