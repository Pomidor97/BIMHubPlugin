using System;
using System.Windows.Input;

namespace BIMHubPlugin.ViewModels
{
    /// <summary>
    /// Простая реализация ICommand для MVVM паттерна
    /// Позволяет связывать методы ViewModel с командами в XAML
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// Создает новую команду
        /// </summary>
        /// <param name="execute">Действие для выполнения</param>
        /// <param name="canExecute">Условие, определяющее можно ли выполнить команду (опционально)</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Определяет, может ли команда выполниться в текущем состоянии
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// Выполняет команду
        /// </summary>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// Событие, которое срабатывает когда изменяется возможность выполнения команды
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Принудительно вызывает перепроверку CanExecute для всех команд
        /// </summary>
        public static void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Типизированная версия RelayCommand для более строгой типизации
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        /// <summary>
        /// Создает новую типизированную команду
        /// </summary>
        /// <param name="execute">Действие для выполнения</param>
        /// <param name="canExecute">Условие, определяющее можно ли выполнить команду (опционально)</param>
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Определяет, может ли команда выполниться в текущем состоянии
        /// </summary>
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
                return true;

            if (parameter == null && typeof(T).IsValueType)
                return false;

            return _canExecute((T)parameter);
        }

        /// <summary>
        /// Выполняет команду
        /// </summary>
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        /// <summary>
        /// Событие, которое срабатывает когда изменяется возможность выполнения команды
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}