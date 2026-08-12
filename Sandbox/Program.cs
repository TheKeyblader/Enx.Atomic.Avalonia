using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;

AppBuilder.Configure<Application>().UsePlatformDetect().Start(AppMain, args);

static void AppMain(Application app, string[] args)
{
    var hashset = new HashSet<AvaloniaProperty>();
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    foreach (var type in assembly.GetTypes())
    foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(type))
        hashset.Add(property);

    var toExport = hashset.Select(x => new
    {
        Name = x.Name,
        Owner = x.OwnerType.AssemblyQualifiedName,
        PropertType = x.PropertyType.AssemblyQualifiedName,
    });

    File.WriteAllText("./exportProps.json",JsonSerializer.Serialize(toExport));
}
