using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Styling;

namespace Enx.Atomic.Avalonia;

public static class EmitterHelpers
{
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

    public enum TypeClass
    {
        BuiltinType,
        AnonymousType,
        ClosureType,
        StructType,
        NormalType,
        TopLevelProgramClosureType,
    }

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

    public const string DarkEndName = "Dark";

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
