using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CmlLib.Core;
using MineLauncher.Commands;
using Version = System.Version;

namespace MineLauncher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : INotifyPropertyChanged
{
    public MinecraftLauncher Cml { get; private set; }
    
    private Account _account;
    public Account Account
    {
        get => _account;
        private set => SetField(ref _account, value);
    }

    public Settings AppSettings { get; private set; }
    public bool CanInstallAny => !string.IsNullOrEmpty(AppSettings.InstallDir);

    public Dictionary<string, Repo> Repos { get; private set; } = new();

    private Repo _selectedRepo;
    public Repo SelectedRepo
    {
        get => _selectedRepo;
        private set
        {
            if (SetField(ref _selectedRepo, value))
            {
                SelectedRepoChanged?.Invoke();
            }
        }
    }
    public event Action SelectedRepoChanged;
    public event Action SelectedRepoTaskChanged;
    
    public Repo RunningRepo { get; private set; }

    public string MinecraftBaseDir => Path.Combine(Instance.AppSettings.InstallDir, "minecraft");
    public string LogsDir => Path.Combine(MinecraftBaseDir, "logs");
    public string LatestLogFile => Path.Combine(LogsDir, "latest.log");

    public Visibility ActionPanelVisibility => MainTab.GetSelectedTabInGroup("MainTabs")?.DataContext is Repo ? Visibility.Visible : Visibility.Collapsed;

    public static App Instance => (App)Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppSettings = new Settings();
            
        AppSettings.InstallDirChanged += () =>
        {
            foreach (var repo in Repos)
            {
                repo.Value.FetchVersion();
            }
        };

        AppSettings.LanguageChanged += () =>
        {
            switch (AppSettings.Language)
            {
                case Language.System:
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InstalledUICulture;
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.InstalledUICulture;
                    break;
                case Language.English:
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    break;
                case Language.Russian:
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
                    break;
            }
            
            var windows = Application.Current.Windows.Cast<Window>();
            foreach (var window in windows)
            {
                // Update the window and all its children
                window.UpdateDefaultStyle();
                Utils.UpdateAllLocalizationBindings(window);
            }
        };
            
        AppSettings.Load();
        
        MainTab.SelectTabByItemName("MainTabs", AppSettings.Repo);
            
        FetchRepos();
        
        if (string.IsNullOrEmpty(AppSettings.Repo))
            MainTab.SelectTabByItemName("MainTabs", Repos.Keys.FirstOrDefault());

        MainTab.GroupItemSelected += (group, item) =>
        {
            if (group == "MainTabs")
            {
                if (item == "account")
                {
                    if (Account is null)
                    {
                        LoginCommand.Execute(null);
                    }
                }
                
                OnPropertyChanged(nameof(ActionPanelVisibility));
            }
        };

        MinecraftLauncherParameters parameters = MinecraftLauncherParameters.CreateDefault(new MinecraftPath(MinecraftBaseDir));
        Cml = new(parameters);
    }

    private ICommand _loginCommand;
    public ICommand LoginCommand => _loginCommand ??= new RelayCommand(async () =>
    {
        await Task.Delay(3000);
        
        Account = new Account
        {
            Username = "Drakosha",
        };
    });
    
    private ICommand _logoutCommand;
    public ICommand LogoutCommand => _logoutCommand ??= new RelayCommand(() =>
    {
        Account = null;

        if (MainTab.GetGroupSelection("MainTabs") == "account")
        {
            MainTab.SelectTabByItemName("MainTabs", Repos.Keys.FirstOrDefault());
        }
    });

    private void RepoTaskChanged(Repo repo)
    {
        if (repo.CurrentTask == Repo.RepoTaskType.Running)
            RunningRepo = repo;
        else if (repo == RunningRepo)
            RunningRepo = null;
        
        if (repo == SelectedRepo)
            SelectedRepoTaskChanged?.Invoke();
    }

    public void FetchRepos()
    {
        Repos = new()
        {
            ["techno_magic"] = new()
            {
                TitleByLanguage = new()
                {
                    [Language.English] = "Techno-Magic",
                    [Language.Russian] = "Техно-Магия"
                },
                Loader = "forge",
                MCVersion = new Version(1, 12, 2),
                LoaderVersion = new Version(14, 23, 5, 2859),
                RepoUrl = "https://github.com/deaddarkus4/techo-magic-1.12.2",
            }
        };

        foreach (var repo in Repos)
        {
            repo.Value.Key = repo.Key;
            repo.Value.FetchVersion();
            repo.Value.CurrentTaskChanged += RepoTaskChanged; 
        }

        if (Repos.TryGetValue(AppSettings.Repo ?? "", out var foundRepo))
            SelectedRepo = foundRepo;
        else
        {
            SelectedRepo = Repos.FirstOrDefault().Value;
            AppSettings.Repo = SelectedRepo.Key;
        }
    }
    
    public string GetHWID()
    {
        // windows key?
        return null;
    }

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