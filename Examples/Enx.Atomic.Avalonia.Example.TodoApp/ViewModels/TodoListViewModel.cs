using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Enx.Atomic.Avalonia.Example.TodoApp.ViewModels;

/// <summary>
/// The whole app state: the todo list, the text being typed, and the active filter. The view binds to
/// <see cref="VisibleTodos"/> rather than the full list, so switching a filter only swaps the collection's
/// contents instead of requiring an <c>ICollectionView</c> (which would need an extra Avalonia package).
/// </summary>
public sealed class TodoListViewModel : ObservableObject
{
    private readonly ObservableCollection<TodoItem> _todos = [];
    private string _newTodoText = string.Empty;
    private TodoFilter _filter = TodoFilter.All;

    public TodoListViewModel() => Refresh();

    /// <summary>The subset currently shown; rebuilt whenever the filter changes or an item leaves the list.</summary>
    public ObservableCollection<TodoItem> VisibleTodos { get; } = [];

    public string NewTodoText
    {
        get => _newTodoText;
        set
        {
            if (SetProperty(ref _newTodoText, value))
                OnPropertyChanged(nameof(CanAdd));
        }
    }

    /// <summary>Whether <see cref="AddTodo"/> would do anything; bound to the Add button's <c>IsEnabled</c>.</summary>
    public bool CanAdd => !string.IsNullOrWhiteSpace(_newTodoText);

    public TodoFilter Filter
    {
        get => _filter;
        set
        {
            if (!SetProperty(ref _filter, value))
                return;

            OnPropertyChanged(nameof(IsAllFilter));
            OnPropertyChanged(nameof(IsActiveFilter));
            OnPropertyChanged(nameof(IsDoneFilter));
            Refresh();
        }
    }

    // The three radio buttons bind two-way to these. A radio button being unchecked by its group reports
    // `false`; only `true` means "the user picked me", so `false` is deliberately ignored.
    public bool IsAllFilter
    {
        get => _filter == TodoFilter.All;
        set
        {
            if (value)
                Filter = TodoFilter.All;
        }
    }

    public bool IsActiveFilter
    {
        get => _filter == TodoFilter.Active;
        set
        {
            if (value)
                Filter = TodoFilter.Active;
        }
    }

    public bool IsDoneFilter
    {
        get => _filter == TodoFilter.Done;
        set
        {
            if (value)
                Filter = TodoFilter.Done;
        }
    }

    public bool IsEmpty => VisibleTodos.Count == 0;

    public bool HasCompletedTodos => _todos.Any(todo => todo.IsDone);

    public string ItemsLeftText
    {
        get
        {
            var count = _todos.Count(todo => !todo.IsDone);
            return count == 1 ? "1 item left" : $"{count} items left";
        }
    }

    public void AddTodo()
    {
        var title = NewTodoText.Trim();
        if (title.Length == 0)
            return;

        var todo = new TodoItem(title);
        todo.PropertyChanged += OnTodoPropertyChanged;
        _todos.Add(todo);
        NewTodoText = string.Empty;
        Refresh();
    }

    public void RemoveTodo(TodoItem todo)
    {
        if (!_todos.Remove(todo))
            return;

        todo.PropertyChanged -= OnTodoPropertyChanged;
        Refresh();
    }

    public void ClearCompleted()
    {
        foreach (var todo in _todos.Where(todo => todo.IsDone).ToList())
        {
            todo.PropertyChanged -= OnTodoPropertyChanged;
            _todos.Remove(todo);
        }

        Refresh();
    }

    private void OnTodoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TodoItem.IsDone))
            return;

        OnPropertyChanged(nameof(HasCompletedTodos));
        OnPropertyChanged(nameof(ItemsLeftText));

        // Under "All", toggling an item doesn't change which items the list shows — rebuilding the collection
        // would recreate every row (and drop focus) for nothing. The item itself already re-renders.
        if (_filter != TodoFilter.All)
            Refresh();
    }

    private void Refresh()
    {
        var filtered = _filter switch
        {
            TodoFilter.Active => _todos.Where(todo => !todo.IsDone),
            TodoFilter.Done => _todos.Where(todo => todo.IsDone),
            _ => _todos,
        };

        VisibleTodos.Clear();
        foreach (var todo in filtered)
            VisibleTodos.Add(todo);

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasCompletedTodos));
        OnPropertyChanged(nameof(ItemsLeftText));
    }
}
