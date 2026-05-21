using System.Linq.Expressions;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CodeGenHelpers;
using ExpressionToCodeLib;
using Microsoft.CodeAnalysis;

namespace Enx.Atomic.Avalonia.CSharp;

public class CSharpEmitterOptions
{
    public string Namespace { get; init; } = "Enx.Atomic";
    public bool EmitStyle { get; init; } = true;
    public string StyleClassName { get; init; } = "AtomicStyles";
    public bool EmitResource { get; init; } = true;
    public string ResourceClassName { get; init; } = "AtomicResourceDictionary";
    public bool EmitResourceEnum { get; init; } = true;
    public string ResourceEnumClassName { get; init; } = "AtomicResourceEnum";
    public bool EmitMarkupExtension { get; init; } = true;
    public string MarkupExtensionClassName { get; init; } = "AtomicResourceExtension";
    public string FileNamePattern { get; init; } = "{0}.g.cs";
}

public class CSharpEmitter<TTheme>(CSharpEmitterOptions Options) : IStyleEmitter<TTheme>
    where TTheme : class
{
    public EmitResult[] Emit(EmitContext<TTheme> context)
    {
        List<EmitResult> results = [];
        if (Options.EmitStyle)
            results.Add(EmitStyles(context));

        if (Options.EmitResource)
            results.Add(EmitResource(context));
        
        if(Options.EmitResourceEnum)
            results.Add(EmitEnum(context));

        return results.ToArray();
    }

    #region Style

    public virtual EmitResult EmitStyles(EmitContext<TTheme> context)
    {
        var builder = CodeBuilder.Create(Options.Namespace);

        var @usings = EmitterHelpers.GetUsings(context.Utils, context.Configuration.Emitters);
        @usings.Add("Avalonia.Styling");

        foreach (var @using in @usings)
            builder.AddNamespaceImport(@using);

        var classBuilder = builder
            .AddClass(Options.StyleClassName)
            .WithAccessModifier(Accessibility.Public)
            .SetBaseClass("Styles");

        var constructorBuilder = classBuilder
            .AddConstructor(Accessibility.Public)
            .WithBody(writer =>
            {
                int index = 0;
                foreach (var util in context.Utils)
                {
                    index++;
                    WriteUtil(writer, index, util, context);
                    writer.AppendLine("");
                }
            });

        return new EmitResult
        {
            Content = builder.Build(),
            FileName = string.Format(Options.FileNamePattern, Options.StyleClassName),
        };
    }

    private void WriteUtil(
        ICodeWriter writer,
        int index,
        StringifiedUtil util,
        EmitContext<TTheme> context
    )
    {
        var varName = "style" + index;
        writer.AppendLine($"var {varName} = new Style({ExpressionToCode.ToCode(util.Selector)});");

        foreach (var setter in util.Body)
        {
            WriteSetter(writer, varName, setter, context);
            writer.AppendLine("");
        }

        if (util.ContainerQuery is not null)
        {
            var containerVarName = "container" + index;
            writer.AppendLine(
                $"var {containerVarName} = new ContainerQuery({ExpressionToCode.ToCode(util.ContainerQuery)}, \"{context.Configuration.ContainerName}\");"
            );
            writer.AppendLine($"{containerVarName}.Add({varName});");
            writer.AppendLine($"Add({containerVarName});");
        }
        else
        {
            writer.AppendLine($"Add({varName});");
        }
    }

    private void WriteSetter(
        ICodeWriter writer,
        string styleVarName,
        Setter setter,
        EmitContext<TTheme> context
    )
    {
        var property = GetAvaloniaPropertyName(setter.Property!);

        var emitter = context.Configuration.Emitters.First(x =>
            x.CanHandle(setter.Value!.GetType())
        );

        var valueStr = emitter.ToCSharpString(setter.Value!, out var valueVarName);

        if (valueVarName is not null)
            writer.AppendLine(valueStr);

        writer.AppendLine(
            $"{styleVarName}.Setters.Add(new Setter({property},{valueVarName ?? valueStr}));"
        );
    }

    private static readonly Dictionary<AvaloniaProperty, string> PropertyAccessors = [];

    private static string GetAvaloniaPropertyName(AvaloniaProperty property)
    {
        if (PropertyAccessors.TryGetValue(property, out var accessor))
            return accessor;

        var fields = property
            .OwnerType.GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.IsInitOnly);

        foreach (var field in fields)
        {
            var value = (AvaloniaProperty)field.GetValue(null)!;

            if (value != property)
                continue;

            PropertyAccessors[property] = accessor = $"{property.OwnerType.Name}.{field.Name}";
            break;
        }

        return accessor ?? throw new InvalidOperationException();
    }

    #endregion

    #region Resource


    public EmitResult EmitResource(EmitContext<TTheme> context)
    {
        var builder = CodeBuilder.Create(Options.Namespace);
        HashSet<string> @usings = [];

        return new EmitResult
        {
            Content = "",
            FileName = string.Format(Options.FileNamePattern, Options.ResourceClassName),
        };
    }

    #endregion


    #region Enum

    public EmitResult EmitEnum(EmitContext<TTheme> context)
    {
        var builder = CodeBuilder.Create(Options.Namespace);

        var enumBuilder = builder.AddEnum(Options.ResourceEnumClassName)
            .MakePublicEnum();

        var themeKeys  = EmitterHelpers.GetThemeKeys(context.Configuration.Theme, context.Configuration.Emitters);
        foreach (var key in themeKeys)
            enumBuilder.AddValue(key);

        return new EmitResult
        {
            Content = enumBuilder.Build(),
            FileName = string.Format(Options.FileNamePattern, Options.ResourceClassName),
        };
    }

    #endregion
}
