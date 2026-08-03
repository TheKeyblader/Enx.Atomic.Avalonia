namespace Enx.Atomic.Avalonia;

/// <summary>A single source file to scan for utility tokens.</summary>
/// <param name="Path">The file's path, used for diagnostics and reporting.</param>
/// <param name="Content">The file's raw text content.</param>
/// <param name="Id">Optional identifier scoping extracted tokens to this file (e.g. for per-file variant matching).</param>
public record SourceFile(string Path, string Content, string? Id = null);
