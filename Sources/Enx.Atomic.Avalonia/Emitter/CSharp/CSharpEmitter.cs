using System.Linq.Expressions;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CodeGenHelpers;
using ExpressionToCodeLib;
using Microsoft.CodeAnalysis;

namespace Enx.Atomic.Avalonia.CSharp;

/// <summary>Configures what <see cref="CSharpEmitter{TTheme}"/> generates and how the generated types are named.</summary>
public class CSharpEmitterOptions
{
    /// <summary>The namespace generated types are placed in.</summary>
    public string Namespace { get; init; } = "Enx.Atomic";

    /// <summary>Whether to generate the <see cref="Avalonia.Styling.Styles"/> subclass containing the resolved utility styles.</summary>
    public bool EmitStyle { get; init; } = true;

    /// <summary>The name of the generated styles class.</summary>
    public string StyleClassName { get; init; } = "AtomicStyles";

    /// <summary>Whether to generate a resource dictionary. Currently a no-op placeholder — see <see cref="EmitResource(EmitContext{TTheme})"/>.</summary>
    public bool EmitResource { get; init; } = true;

    /// <summary>The name of the generated resource dictionary class.</summary>
    public string ResourceClassName { get; init; } = "AtomicResourceDictionary";

    /// <summary>Whether to generate an enum listing every resource key discovered on the theme.</summary>
    public bool EmitResourceEnum { get; init; } = true;

    /// <summary>The name of the generated resource-key enum.</summary>
    public string ResourceEnumClassName { get; init; } = "AtomicResourceEnum";

    /// <summary>Whether to generate a markup extension for referencing theme resources from XAML.</summary>
    public bool EmitMarkupExtension { get; init; } = true;

    /// <summary>The name of the generated markup extension class.</summary>
    public string MarkupExtensionClassName { get; init; } = "AtomicResourceExtension";

    /// <summary>
    /// <see cref="string.Format(string, object?)"/> pattern used to derive each generated file's name from
    /// its class name, e.g. the default <c>"{0}.g.cs"</c> yields <c>AtomicStyles.g.cs</c>.
    /// </summary>
    public string FileNamePattern { get; init; } = "{0}.g.cs";
}

/// <summary>
/// Emits the resolved atomic styles as generated C# source: a <see cref="Avalonia.Styling.Styles"/> subclass
/// that constructs each <see cref="Setter"/> directly (avoiding XAML parsing at runtime), plus an enum of the
/// theme's resource keys. Controlled by <see cref="CSharpEmitterOptions"/>.
/// </summary>
public class CSharpEmitter<TTheme>(CSharpEmitterOptions Options) : IStyleEmitter<TTheme>
    where TTheme : class
{
    /// <summary>Generates the files enabled by <see cref="CSharpEmitterOptions"/>: the styles class, resource dictionary, and/or resource enum.</summary>
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

    /// <summary>Generates the <see cref="Avalonia.Styling.Styles"/> subclass whose constructor builds and adds one <see cref="Style"/> per resolved util.</summary>
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

    /// <summary>Writes the local variable declaration and setter/container-query statements for a single resolved util.</summary>
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

    /// <summary>Writes a single <c>Setters.Add(new Setter(...))</c> statement, using the matching <see cref="ValueEmitter"/> to render the value.</summary>
    /// <exception cref="InvalidOperationException">No configured <see cref="ValueEmitter"/> can handle the setter's value type.</exception>
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

    /// <summary>Resolves the static field that declares <paramref name="property"/> (e.g. <c>"Button.BackgroundProperty"</c>) via reflection, caching the result.</summary>
    /// <exception cref="InvalidOperationException">No static field on the property's owner type holds this property instance.</exception>
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

    /// <summary>Generates the resource dictionary file. Currently a placeholder that emits no content.</summary>
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

    /// <summary>Generates a public enum listing every resource key discovered on the theme via <see cref="EmitterHelpers.GetThemeKeys"/>.</summary>
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
