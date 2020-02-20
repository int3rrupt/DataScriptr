using System;
using System.Windows.Input;

namespace MVVMBase
{
    public class RelayCommand : ICommand
    {
        private Action<object> _methodToExecute;

        private Func<bool> _canExecuteEvaluator;

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public RelayCommand(Action<object> methodToExecute, Func<bool> canExecuteEvaluator)
        {
            _methodToExecute = methodToExecute;
            _canExecuteEvaluator = canExecuteEvaluator;
        }

        public RelayCommand(Action<object> methodToExecute)
            : this(methodToExecute, null)
        {
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteEvaluator?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _methodToExecute?.Invoke(parameter);
        }
    }
}