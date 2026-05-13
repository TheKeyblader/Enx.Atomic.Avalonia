using System.Reflection;
using Avalonia;
using CodeGenHelpers;
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
    public string ResourceEnumClassName { get; init; } = "AtomicResource";
    public bool EmitMarkupExtension { get; init; } = true;
    public string MarkupExtensionClassName { get; init; } = "AtomicResourceExtension";
    public string FileNamePattern { get; init; } = "{ClassName}.g.cs";
}

public class CSharpEmitter<TTheme>(CSharpEmitterOptions Options) : IStyleEmitter<TTheme>
    where TTheme : class
{
    public EmitResult[] Emit(EmitContext<TTheme> context)
    {
        List<EmitResult> results = [];
        if (Options.EmitStyle)
            results.Add(EmitStyles(context));

        return results.ToArray();
    }

    #region Style

    protected virtual EmitResult EmitStyles(EmitContext<TTheme> context)
    {
        var builder = CodeBuilder.Create(Options.Namespace);
        
        var classBuilder = builder
            .AddClass(Options.StyleClassName)
            .WithAccessModifier(Accessibility.Public)
            .SetBaseClass("Styles");

        var constructorBuilder = classBuilder
            .AddConstructor(Accessibility.Public);

        return new EmitResult
        {
            Content = builder.Build(),
            FileName = string.Format(Options.FileNamePattern, Options.StyleClassName)
        };
    }

    private static readonly Dictionary<AvaloniaProperty, string> PropertyAccessors = [];

    public static string GetAvaloniaPropertyName(AvaloniaProperty property)
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
}
