using System.Windows.Controls;
using System.Windows.Input;

namespace MineLauncher;

public partial class SettingsScreen : UserControl
{
    public Settings Settings => App.Instance.AppSettings;
    
    public SettingsScreen()
    {
        InitializeComponent();
    }

    private ICommand _browseInstallDir;
    public ICommand BrowseInstallDir => _browseInstallDir ??= new RelayCommand(() =>
    {
        System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            SelectedPath = App.Instance.AppSettings.InstallDir
        };
            
        var result = dialog.ShowDialog();  
        if (result.ToString() != string.Empty)  
        {  
            App.Instance.AppSettings.InstallDir = dialog.SelectedPath;  
        }
    });
    
    private ICommand _browseJavaPath;
    public ICommand BrowseJavaPath => _browseJavaPath ??= new RelayCommand(() =>
    {
        System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog
        {
            FileName = App.Instance.AppSettings.JavaPath,
            Filter = "executable files (*.exe)|*.exe"
        };
            
        var result = dialog.ShowDialog();  
        if (result.ToString() != string.Empty)  
        {  
            App.Instance.AppSettings.JavaPath = dialog.FileName;  
        }
    });
}