using System.Windows.Input;

namespace Cad_Point_Manager.Commands
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;

            // If binding didn't provide a parameter, WPF passes null.
            if (parameter is null) return _canExecute(default);

            // If WPF gives you the wrong type, treat it as not executable.
            if (parameter is T t) return _canExecute(t);

            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is null)
            {
                _execute(default);
                return;
            }

            if (parameter is T t)
                _execute(t);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void NotifyCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void NotifyCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
