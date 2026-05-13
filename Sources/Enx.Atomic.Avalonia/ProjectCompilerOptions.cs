using System.IO.Abstractions;
using Testably.Abstractions;

namespace Enx.Atomic.Avalonia;

public class ProjectCompilerOptions
{
    public required string[] ProjectPaths { get; init; }
    public required string OutputDirectory { get; init; }
    public required IReadOnlyList<string> IncludePatterns { get; init; } =
    ["**/*.axaml", "**/*.cs"];
    public required IReadOnlyList<string> ExcludePatterns { get; init; } =
    ["**/bin/**", "**/obj/**"];
    public IFileSystem FileSystem { get; init; } = new RealFileSystem();
}
