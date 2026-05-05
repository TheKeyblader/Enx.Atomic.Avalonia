using Avalonia.Styling;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;

namespace Enx.Atomic.Avalonia;

public static class StyleEmitter
{
    public string Generate<TTheme>(
        AtomicConfiguration<TTheme> configuration,
        StringifiedUtil<TTheme>[] utils
    )
        where TTheme : class
    {
        var builder = CodeBuilder.Create(configuration.Namespace);

        HashSet<string> usings = ["Avalonia.Styling"];

        var valueTypes = utils
            .SelectMany(x => x.Body)
            .Select(x => x.Property!.PropertyType)
            .Distinct()
            .ToArray();

        foreach (var valueType in valueTypes)
        {
            var emitter =
                configuration.Emitters.FirstOrDefault(x => x.CanHandle(valueType))
                ?? throw new InvalidOperationException($"No emitter for type {valueType}");

            foreach (var @using in emitter.GetUsings())
                usings.Add(@using);
        }

        var ownerTypes = utils
            .SelectMany(x => x.Body)
            .Select(x => x.Property!.OwnerType)
            .Distinct()
            .ToArray();

        foreach (var ownerType in ownerTypes)
        {
            if (ownerType.Namespace is not null)
                usings.Add(ownerType.Namespace);
        }

        foreach (var @using in usings)
            builder.AddNamespaceImport(@using);

        var classBuilder = builder
            .AddClass(configuration.StyleClassName)
            .WithAccessModifier(Accessibility.Public)
            .SetBaseClass("Styles");

        var constructorBuilder = classBuilder.AddConstructor(Accessibility.Public)
            .WithBody(writer =>
            {

                int index = 0;
                foreach (var util in utils)
                {
                    var varName = "style" + index;
                    writer.AppendLine($"var {varName} = new Style({util.Selector});");
                }
            });

        return builder.Build();
    }
}
