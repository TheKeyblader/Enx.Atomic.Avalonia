using System;
using Avalonia;
using Avalonia.Styling;
using Enx.Atomic.Avalonia;
using Enx.Atomic.Avalonia.Preset.Mini;

AppBuilder.Configure<Application>().UsePlatformDetect().Start(AppMain, args);

static void AppMain(Application app, string[] args)
{
    var builder = ThemeBuilder<MiniTheme>.Create();
    var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };
    builder.AddMiniTheme(configuration);

    Console.WriteLine($"rules: {configuration.Rules.Count}, variants: {configuration.Variants.Count}");
    Console.WriteLine(
        $"spacing: {configuration.Theme.Spacing.Count}, colors: {configuration.Theme.Colors.Count}, "
            + $"radii: {configuration.Theme.Radii.Count}, breakpoints: {configuration.Theme.Breakpoints.Count}"
    );
    Console.WriteLine();

    // calling twice should not double-register anything (AddRuleOnce/AddVariantOnce/AddStaticRuleOnce)
    builder.AddMiniTheme(configuration);
    Console.WriteLine($"after re-running AddMiniTheme -> rules: {configuration.Rules.Count}, variants: {configuration.Variants.Count}");
    Console.WriteLine();

    var generator = new AtomicGenerator<MiniTheme>(configuration);

    string[] tokens =
    [
        // static rules
        "hidden",
        "cursor-pointer",
        "flex-row",
        "text-center",
        "italic",
        "font-bold",
        "truncate",
        "object-cover",
        // spacing (dictionary hit + negative)
        "m-4",
        "-m-4",
        "mx-8",
        "p-2",
        "gap-4",
        "gap-x-2",
        // spacing rem fallback (13 isn't in the default scale -> 13 * 16px)
        "m-13",
        // size / radius
        "w-sm",
        "min-h-lg",
        "rounded-lg",
        "rounded-t-xl",
        "rounded-full",
        // border width vs color ambiguity (both share the "border-" prefix)
        "border",
        "border-2",
        "border-red-500",
        // text size vs color ambiguity (both share the "text-" prefix)
        "text-sm",
        "text-red-500",
        // colors
        "bg-blue-500",
        // variants
        "hover:bg-red-500",
        "disabled:text-gray-500",
        "hover:focus:underline",
        "sm:flex-row",
        "max-sm:hidden",
        // should not match anything
        "not-a-real-class",
    ];

    foreach (var token in tokens)
    {
        var results = generator.ParseToken(token);
        Console.WriteLine($"{token} -> {results.Length} result(s)");

        foreach (var result in results)
        {
            var selector = result.Selector.Compile()(null!);
            Console.WriteLine($"    selector: {selector}");

            if (result.ContainerQuery is not null)
            {
                var query = result.ContainerQuery.Compile()(null!);
                Console.WriteLine($"    container: {query}");
            }

            foreach (var setter in result.Body)
                Console.WriteLine($"    {setter.Property} = {setter.Value}");
        }
    }
}
