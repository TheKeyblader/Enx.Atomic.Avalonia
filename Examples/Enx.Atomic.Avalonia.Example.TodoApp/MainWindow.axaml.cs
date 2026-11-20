using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Enx.Atomic.Avalonia.Example.TodoApp.ViewModels;

namespace Enx.Atomic.Avalonia.Example.TodoApp;

public partial class MainWindow : Window
{
    private readonly TodoListViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void AddTodo_Click(object? sender, RoutedEventArgs e) => AddTodo();

    private void NewTodoBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        AddTodo();
        e.Handled = true;
    }

    private void DeleteTodo_Click(object? sender, RoutedEventArgs e)
    {
        // The button lives in the item's DataTemplate, so its DataContext is the item itself.
        if (sender is Control { DataContext: TodoItem todo })
            _viewModel.RemoveTodo(todo);
    }

    private void ClearCompleted_Click(object? sender, RoutedEventArgs e) => _viewModel.ClearCompleted();

    private void AddTodo()
    {
        _viewModel.AddTodo();
        NewTodoBox.Focus();
    }
}
