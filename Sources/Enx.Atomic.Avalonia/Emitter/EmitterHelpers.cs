using Avalonia;
using Avalonia.Styling;

namespace Enx.Atomic.Avalonia;

public static class EmitterHelpers
{
    public static HashSet<string> GetUsings(StringifiedUtil[] utils, List<ValueEmitter> emitters)
    {
        HashSet<string> usings = ["Avalonia.Styling"];

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
}
