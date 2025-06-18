using System;
using System.Windows.Input;

namespace MineLauncher.Commands;

public abstract class Command(Func<string> nameFunc) : ICommand
{
    public string Name => nameFunc();

    public abstract bool CanExecute(object parameter);
    public abstract void Execute(object parameter);
    public abstract event EventHandler CanExecuteChanged;
}