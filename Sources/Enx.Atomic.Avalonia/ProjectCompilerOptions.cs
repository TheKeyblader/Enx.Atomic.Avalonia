using System.IO.Abstractions;
using Testably.Abstractions;

namespace Enx.Atomic.Avalonia;

/// <summary>Configures a <see cref="ProjectCompiler{TTheme}"/>: where to look for source files and where to write generated output.</summary>
public class ProjectCompilerOptions
{
    /// <summary>Root directories or project files to scan for source files.</summary>
    public required string[] ProjectPaths { get; init; }

    /// <summary>Directory that emitted output files are written to.</summary>
    public required string OutputDirectory { get; init; }

    /// <summary>Glob patterns (relative to each entry in <see cref="ProjectPaths"/>) identifying files to scan. Defaults to XAML and C# files.</summary>
    public required IReadOnlyList<string> IncludePatterns { get; init; } =
    ["**/*.axaml", "**/*.cs"];

    /// <summary>Glob patterns identifying files to skip even if they match <see cref="IncludePatterns"/>. Defaults to build output directories.</summary>
    public required IReadOnlyList<string> ExcludePatterns { get; init; } =
    ["**/bin/**", "**/obj/**"];

    /// <summary>The file system used to read source files and write output. Overridable for testing.</summary>
    public IFileSystem FileSystem { get; init; } = new RealFileSystem();
}
