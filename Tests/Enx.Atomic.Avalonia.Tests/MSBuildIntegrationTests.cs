using System.Diagnostics;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// Integration coverage for <c>Enx.Atomic.Avalonia.CodeGen.targets</c>: builds a real, throwaway SDK-style
/// project pair with <c>dotnet build</c> — outside the repo tree, referencing the real <c>Sources/</c> projects
/// by absolute path — and asserts on the actual generated output. Exists so the MSBuild wiring's correctness
/// no longer depends on the <c>Examples/</c> projects staying correct by inspection (see the "A more robust,
/// better-tested MSBuild build system" Roadmap item in the root README): a regression in incrementality, the
/// self-scanning/<c>CS2002</c> exclusion, or the output path defaults would fail one of these tests instead of
/// only showing up if someone happens to notice a stale/duplicated <c>Examples/</c> build.
/// </summary>
public sealed class MSBuildIntegrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "enx-atomic-msbuild-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // Best effort — a file locked by a lingering MSBuild/dotnet process shouldn't fail the test run.
        }
    }

    [Fact]
    public void Build_GeneratesStylesFromScannedSource()
    {
        var (appDir, _) = Scaffold();

        var (exitCode, output) = DotnetBuild(appDir);

        Assert.True(exitCode == 0, output);
        Assert.DoesNotContain("CS2002", output);

        var generated = ReadGenerated(appDir, output);
        Assert.Contains("class AtomicStyles", generated);
        Assert.Contains("flex-row", generated);
    }

    [Fact]
    public void Rebuild_WithNoSourceChanges_LeavesOutputUntouched()
    {
        // EnxAtomicGenerateStyles has no Inputs/Outputs of its own — it runs on every build (see the comment
        // in the .targets file for why an MSBuild-level staleness check turned out not to be reliable here).
        // What actually has to stay stable across a no-op rebuild is the *output file's mtime*, since that's
        // what lets the app project's own CoreCompile skip recompiling GenStyles.g.cs — AtomicCli's
        // write-if-different is what's really under test.
        var (appDir, _) = Scaffold();
        var (firstExitCode, firstOutput) = DotnetBuild(appDir);
        Assert.True(firstExitCode == 0, firstOutput);

        var generatedPath = GeneratedPath(appDir);
        var firstWrite = File.GetLastWriteTimeUtc(generatedPath);

        var (exitCode, output) = DotnetBuild(appDir);

        Assert.True(exitCode == 0, output);
        Assert.Contains("unchanged", output);
        Assert.DoesNotContain("Enx.Atomic.Avalonia.CodeGen: wrote", output);
        Assert.Equal(firstWrite, File.GetLastWriteTimeUtc(generatedPath));
    }

    [Fact]
    public void Rebuild_AfterSourceChange_Regenerates()
    {
        var (appDir, _) = Scaffold();
        var (firstExitCode, firstOutput) = DotnetBuild(appDir);
        Assert.True(firstExitCode == 0, firstOutput);

        var generatedPath = GeneratedPath(appDir);
        var firstWrite = File.GetLastWriteTimeUtc(generatedPath);

        // A real filesystem can have coarser mtime resolution than this loop's execution time — make sure the
        // new write is actually observably later before asserting on it.
        Thread.Sleep(50);
        File.WriteAllText(Path.Combine(appDir, "Widgets.cs"), WidgetsSource("flex-row bg-blue-500 underline"));

        var (exitCode, output) = DotnetBuild(appDir);

        Assert.True(exitCode == 0, output);
        Assert.Contains("Enx.Atomic.Avalonia.CodeGen: wrote", output);

        var generated = ReadGenerated(appDir, output);
        Assert.Contains("underline", generated);
        Assert.NotEqual(firstWrite, File.GetLastWriteTimeUtc(generatedPath));
    }

    [Fact]
    public void Rebuild_AfterConfigProjectRuleChange_Regenerates()
    {
        // Regression test: since EnxAtomicGenerateStyles always runs (see the .targets file's comment for why
        // it doesn't try to infer staleness from the config project's build output timestamp), editing a
        // rule/theme in the config project — the whole point of having one — must still change GenStyles.g.cs
        // even though the app project's own sources are untouched. Overrides the Spacing scale so "p-4"
        // (already in Widgets.cs) resolves to a different, distinctive value.
        var (appDir, configDir) = Scaffold();
        var (firstExitCode, firstOutput) = DotnetBuild(appDir);
        Assert.True(firstExitCode == 0, firstOutput);

        var generatedPath = GeneratedPath(appDir);
        var firstWrite = File.GetLastWriteTimeUtc(generatedPath);
        Assert.DoesNotContain("999", File.ReadAllText(generatedPath));

        Thread.Sleep(50);
        File.WriteAllText(
            Path.Combine(configDir, "Program.cs"),
            """
            using Avalonia;
            using Avalonia.Headless;
            using Enx.Atomic.Avalonia;
            using Enx.Atomic.Avalonia.CodeGen;
            using Enx.Atomic.Avalonia.Preset.Mini;

            AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();

            var builder = ThemeBuilder<MiniTheme>.Create();
            var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };
            builder.AddMiniTheme(configuration);
            builder.Theme.Spacing["4"] = 999f;

            return AtomicCli.Run(args, configuration);
            """
        );

        var (exitCode, output) = DotnetBuild(appDir);

        Assert.True(exitCode == 0, output);
        Assert.Contains("Enx.Atomic.Avalonia.CodeGen: wrote", output);
        Assert.NotEqual(firstWrite, File.GetLastWriteTimeUtc(generatedPath));
        Assert.Contains("999", File.ReadAllText(generatedPath));
    }

    private static string GeneratedPath(string appDir) => Path.Combine(appDir, "GeneratedStyles", "GenStyles.g.cs");

    private static string ReadGenerated(string appDir, string buildOutput)
    {
        var generatedPath = GeneratedPath(appDir);
        Assert.True(File.Exists(generatedPath), $"Expected '{generatedPath}' to exist.\n\n{buildOutput}");
        return File.ReadAllText(generatedPath);
    }

    /// <summary>Writes a throwaway config project + app project pair (mirroring <c>Examples/</c>) under <see cref="_root"/>, referencing this repo's real <c>Sources/</c> projects by absolute path, and returns both directories.</summary>
    private (string AppDir, string ConfigDir) Scaffold()
    {
        var configDir = Path.Combine(_root, "Config");
        var appDir = Path.Combine(_root, "App");
        Directory.CreateDirectory(configDir);
        Directory.CreateDirectory(appDir);

        var repoRoot = RepoRoot();
        var sourcesDir = Path.Combine(repoRoot, "Sources");
        var targetsPath = Path.Combine(sourcesDir, "Enx.Atomic.Avalonia.CodeGen", "build", "Enx.Atomic.Avalonia.CodeGen.targets");
        var engineProj = Path.Combine(sourcesDir, "Enx.Atomic.Avalonia", "Enx.Atomic.Avalonia.csproj");
        var miniProj = Path.Combine(sourcesDir, "Enx.Atomic.Avalonia.Preset.Mini", "Enx.Atomic.Avalonia.Preset.Mini.csproj");
        var codeGenProj = Path.Combine(sourcesDir, "Enx.Atomic.Avalonia.CodeGen", "Enx.Atomic.Avalonia.CodeGen.csproj");

        File.WriteAllText(
            Path.Combine(configDir, "ThrowawayConfig.csproj"),
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>
                <ItemGroup>
                    <ProjectReference Include="{engineProj}" />
                    <ProjectReference Include="{miniProj}" />
                    <ProjectReference Include="{codeGenProj}" />
                </ItemGroup>
                <ItemGroup>
                    <PackageReference Include="Avalonia.Headless" Version="12.1.1" />
                </ItemGroup>
            </Project>
            """
        );

        // Mirrors Examples/Enx.Atomic.Avalonia.Example.Config/Program.cs.
        File.WriteAllText(
            Path.Combine(configDir, "Program.cs"),
            """
            using Avalonia;
            using Avalonia.Headless;
            using Enx.Atomic.Avalonia;
            using Enx.Atomic.Avalonia.CodeGen;
            using Enx.Atomic.Avalonia.Preset.Mini;

            AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();

            var builder = ThemeBuilder<MiniTheme>.Create();
            var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };
            builder.AddMiniTheme(configuration);

            return AtomicCli.Run(args, configuration);
            """
        );

        File.WriteAllText(
            Path.Combine(appDir, "ThrowawayApp.csproj"),
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>
                <PropertyGroup>
                    <EnxAtomicConfigProject>{Path.Combine(configDir, "ThrowawayConfig.csproj")}</EnxAtomicConfigProject>
                </PropertyGroup>
                <ItemGroup>
                    <PackageReference Include="Avalonia" Version="12.1.1" />
                </ItemGroup>
                <Import Project="{targetsPath}" />
            </Project>
            """
        );
        File.WriteAllText(
            Path.Combine(appDir, "Widgets.cs"),
            WidgetsSource("flex-row p-4 rounded-md bg-blue-500 hover:bg-blue-600 dark:bg-slate-800")
        );

        return (appDir, configDir);
    }

    private static string WidgetsSource(string classes) =>
        $$"""
        internal static class Widgets
        {
            public const string Classes = "{{classes}}";
        }
        """;

    private static (int ExitCode, string Output) DotnetBuild(string projectDir)
    {
        var startInfo = new ProcessStartInfo("dotnet", "build --nologo")
        {
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start 'dotnet build'.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, stdout + stderr);
    }

    private static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Enx.Atomic.Avalonia.slnx")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException("Could not locate the repository root ('Enx.Atomic.Avalonia.slnx') above the test assembly.");
    }
}
