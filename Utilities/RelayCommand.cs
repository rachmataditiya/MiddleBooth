using System;
using System.Windows.Input;

namespace MiddleBooth.Utilities
{
    public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => canExecute == null || canExecute(parameter);

        public void Execute(object? parameter) => execute(parameter);
    }

    public class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => canExecute == null || canExecute((T?)parameter);

        public void Execute(object? parameter) => execute((T?)parameter);
    }
}