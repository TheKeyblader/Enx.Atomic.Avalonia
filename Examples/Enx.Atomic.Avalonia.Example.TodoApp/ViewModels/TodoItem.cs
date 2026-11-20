using System.ComponentModel;

namespace Enx.Atomic.Avalonia.Example.TodoApp.ViewModels;

/// <summary>
/// One todo. <see cref="IsDone"/> is the only mutable state: toggling it is what the list view-model listens to
/// so the footer counters and the active/done filters stay in sync.
/// </summary>
public sealed class TodoItem : ObservableObject
{
    private bool _isDone;

    public TodoItem(string title) => Title = title;

    public string Title { get; }

    public bool IsDone
    {
        get => _isDone;
        set => SetProperty(ref _isDone, value);
    }
}
