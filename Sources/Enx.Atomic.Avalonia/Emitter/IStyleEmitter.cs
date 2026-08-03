namespace Enx.Atomic.Avalonia;

/// <summary>
/// Turns the resolved styles produced by an <see cref="AtomicGenerator{TTheme}"/> into their final form —
/// e.g. generated C# source (<see cref="Enx.Atomic.Avalonia.CSharp.CSharpEmitter{TTheme}"/>) or a runtime
/// resource dictionary. Invoked by <see cref="ProjectCompiler{TTheme}"/> as the last step of compilation.
/// </summary>
public interface IStyleEmitter<TTheme>
    where TTheme : class
{
    /// <summary>Produces the output file(s) for the styles in <paramref name="context"/>.</summary>
    EmitResult[] Emit(EmitContext<TTheme> context);
}
