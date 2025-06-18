using System;
using System.Windows.Input;

namespace MineLauncher.Commands;

public class RelayCommand : Command
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null, Func<string> nameFunc = null) : base(nameFunc)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public override event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public override bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

    public override void Execute(object parameter) => _execute();
}