using System.Reflection;
using Avalonia;
using CodeGenHelpers;
using ExpressionToCodeLib;
using Microsoft.CodeAnalysis;

namespace Enx.Atomic.Avalonia;

public static class StyleEmitter
{
    public static string Generate<TTheme>(
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

        usings.Remove(configuration.Namespace);
        foreach (var @using in usings)
            builder.AddNamespaceImport(@using);

        var classBuilder = builder
            .AddClass(configuration.StyleClassName)
            .WithAccessModifier(Accessibility.Public)
            .SetBaseClass("Styles");

        var constructorBuilder = classBuilder
            .AddConstructor(Accessibility.Public)
            .WithBody(writer =>
            {
                int index = 0;
                foreach (var util in utils)
                {
                    index++;
                    var varName = "style" + index;
                    writer.AppendLine(
                        $"var {varName} = new Style({ExpressionToCode.ToCode(util.Selector)});"
                    );

                    foreach (var setter in util.Body)
                    {
                        var property = GetAvaloniaPropertyName(setter.Property!);
                        var emitter = configuration.Emitters.First(x =>
                            x.CanHandle(setter.Value!.GetType())
                        );
                        var valueStr = emitter.ToCSharpString(setter.Value!, out var valueVarName);
                        if (valueVarName is not null)
                            writer.AppendLine(valueStr);
                        writer.AppendLine(
                            $"{varName}.Setters.Add(new Setter({property},{valueVarName ?? valueStr}));"
                        );
                    }

                    if (util.ContainerQuery is not null)
                    {
                        var containerVarName = "container" + 1;
                        writer.AppendLine(
                            $"var {containerVarName} = new ContainerQuery({ExpressionToCode.ToCode(util.ContainerQuery)}, \"{configuration.ContainerName}\");"
                        );
                        writer.AppendLine($"{containerVarName}.Add({varName});");
                        writer.AppendLine($"Add({containerVarName});");
                    }
                    else
                    {
                        writer.AppendLine($"Add({varName});");
                    }

                    writer.AppendLine("");
                }
            });

        var code = builder.Build();
        return code;
    }


}
