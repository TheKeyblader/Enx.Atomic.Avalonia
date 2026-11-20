using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Enx.Atomic.Avalonia.Example.TodoApp.ViewModels;

/// <summary>
/// Minimal <see cref="INotifyPropertyChanged"/> base — an example app doesn't need a toolkit, only a way for
/// bindings to observe changes.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
