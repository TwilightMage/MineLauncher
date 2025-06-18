using System;
using System.Windows.Input;

namespace MineLauncher.Commands;

public class FakeCommand(Func<string> nameFunc) : Command(nameFunc)
{
    public override event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public override bool CanExecute(object parameter) => false;

    public override void Execute(object parameter)
    { }
}