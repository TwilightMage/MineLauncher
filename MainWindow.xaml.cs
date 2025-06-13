using System.Windows;

namespace MineLauncher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
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

    private void BrowseClicked(object sender, RoutedEventArgs e)
    {
        System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
        folderDialog.SelectedPath = App.Instance.AppSettings.InstallDir;
            
        var result = folderDialog.ShowDialog();  
        if (result.ToString() != string.Empty)  
        {  
            App.Instance.AppSettings.InstallDir = folderDialog.SelectedPath;  
        }  
    }
}