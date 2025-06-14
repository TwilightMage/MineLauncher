using System.Windows;

namespace MineLauncher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public static readonly DependencyProperty SelectedScreenProperty =
        DependencyProperty.Register(nameof(SelectedScreen), typeof(object), typeof(MainWindow), 
            new PropertyMetadata(null));

    public object SelectedScreen
    {
        get { return GetValue(SelectedScreenProperty); }
        set { SetValue(SelectedScreenProperty, value); }
    }
    
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ActionClicked(object sender, RoutedEventArgs e)
    {
        if (App.Instance.SelectedRepo.IsUpToDate)
            App.Instance.SelectedRepo.Run();
        else
            App.Instance.SelectedRepo.Update();
    }

    private void ModpackTabChecked(object sender, RoutedEventArgs e)
    {
        int i = 1;
    }
}