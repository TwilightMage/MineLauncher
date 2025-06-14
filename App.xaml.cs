using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using CmlLib.Core;
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

    public Dictionary<string, Repo> Repos { get; private set; } = new();
    public Repo SelectedRepo { get; set; }
        
    public Dictionary<string, string> Javas = new();
        
    public string MinecraftBaseDir => Path.Combine(Instance.AppSettings.InstallDir, "minecraft");

    public Visibility ActionPanelVisibility => MainTab.GetSelectedTabInGroup("MainTabs")?.DataContext is Repo ? Visibility.Visible : Visibility.Collapsed;

    public static App Instance => (App)Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
            
        AppSettings = new Settings();
            
        AppSettings.OnInstallDirChanged += () =>
        {
            foreach (var repo in Repos)
            {
                repo.Value.FetchVersion();
            }
        };

        AppSettings.OnLanguageChanged += () =>
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
        
        LoginCommand.Execute(null);
            
        FetchRepos();

        MainTab.GroupItemSelected += (group, item) =>
        {
            OnPropertyChanged(nameof(ActionPanelVisibility));
        };

        MinecraftLauncherParameters parameters = MinecraftLauncherParameters.CreateDefault(new MinecraftPath(MinecraftBaseDir));
        Cml = new(parameters);
    }

    private ICommand _loginCommand;
    public ICommand LoginCommand => _loginCommand ??= new RelayCommand(() =>
    {
        Account = new Account
        {
            Username = "Drakosha",
        };
    });
    
    private ICommand _logoutCommand;
    public ICommand LogoutCommand => _logoutCommand ??= new RelayCommand(() =>
    {
        Account = null;
    });

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