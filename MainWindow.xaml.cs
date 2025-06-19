using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MineLauncher.Commands;

namespace MineLauncher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : INotifyPropertyChanged
{
    public MainWindow()
    {
        InitializeComponent();
        
        App.Instance.SelectedRepoChanged += () => OnPropertyChanged(nameof(ActionCommand));
        App.Instance.SelectedRepoTaskChanged += () => OnPropertyChanged(nameof(ActionCommand));
    }
    
    private readonly FakeCommand _fetchingCommand = new FakeCommand(
        () => Properties.Strings.Action_Fetching);
    private readonly RelayCommand _installCommand = new RelayCommand(
        () => App.Instance.SelectedRepo?.Update(),
        () => App.Instance.CanInstallAny,
        () => Properties.Strings.Action_Install);
    private readonly RelayCommand _updateCommand = new RelayCommand(
        () => App.Instance.SelectedRepo?.Update(),
        () => App.Instance.CanInstallAny,
        () => Properties.Strings.Action_Update);
    private readonly RelayCommand _cancelUpdateCommand = new RelayCommand(
        () => App.Instance.SelectedRepo?.CancelCurrentTask(),
        null,
        () => Properties.Strings.Action_Cancel);
    private readonly RelayCommand _runCommand = new RelayCommand(
        () => Task.Run(() => App.Instance.SelectedRepo?.Run()),
        null,
        () => Properties.Strings.Action_Run);
    private readonly RelayCommand _stopCommand = new RelayCommand(
        () => App.Instance.SelectedRepo?.CancelCurrentTask(),
        null,
        () => Properties.Strings.Action_Stop);

    public Command ActionCommand => App.Instance.SelectedRepo?.CurrentTask switch
    {
        Repo.RepoTaskType.None => App.Instance.SelectedRepo.IsUpToDate
            ? _runCommand
            : App.Instance.SelectedRepo.GitRepo is null
                ? _installCommand
                : _updateCommand,
        Repo.RepoTaskType.Fetching => _fetchingCommand,
        Repo.RepoTaskType.Updating => _cancelUpdateCommand,
        Repo.RepoTaskType.Running => _stopCommand,
        _ => null
    };
        
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}