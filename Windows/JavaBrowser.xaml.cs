using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using MineLauncher.Commands;

namespace MineLauncher;

public partial class JavaBrowser : Window
{
    public IEnumerable<string> JavaPaths { get; } = JavaUtils.FindJavaPaths();
    
    public string SelectedPath { get; set; }

    public JavaBrowser()
    {
        InitializeComponent();
    }
    
    private void ListViewDoubleClicked(object sender, MouseButtonEventArgs e)
    {
        OkCommand.Execute(null);
    }
    
    private ICommand _okCommand;
    public ICommand OkCommand => _okCommand ??= new RelayCommand(() =>
    {
        DialogResult = true;
        Close();
    });
    
    private ICommand _cancelCommand;
    public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(() =>
    {
        DialogResult = false;
        Close();
    });
}