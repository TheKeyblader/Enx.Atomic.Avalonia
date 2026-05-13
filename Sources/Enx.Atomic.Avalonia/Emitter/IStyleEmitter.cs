namespace Enx.Atomic.Avalonia;

public interface IStyleEmitter<TTheme>
    where TTheme : class
{
    EmitResult[] Emit(EmitContext<TTheme> context);
}
