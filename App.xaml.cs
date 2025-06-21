using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CmlLib.Core;
using MineLauncher.Commands;
using Version = System.Version;

namespace MineLauncher;

public class VersionConverter : JsonConverter<Version>
{
    public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string versionString = reader.GetString();
        return Version.Parse(versionString);
    }

    public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}


/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : INotifyPropertyChanged
{
    public MinecraftLauncher Cml { get; private set; }
    
    private FileSystemWatcher _watcher;
    private Timer _watcherTimer;
    
    private Account _account;
    public Account Account
    {
        get => _account;
        private set => SetField(ref _account, value);
    }

    public Settings AppSettings { get; private set; }
    public bool CanInstallAny => !string.IsNullOrEmpty(AppSettings.InstallDir);

    public Dictionary<string, Repo> Repos { get; } = new();

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
    public event Action SelectedRepoStateChanged;
    
    public LogFileWatcher LogWatcher { get; private set; }
    
    public Repo RunningRepo { get; private set; }

    public string MinecraftBaseDir => Path.Combine(AppSettings.InstallDir ?? ExeDir, "minecraft");
    public string LogsDir => Path.Combine(MinecraftBaseDir, "logs");
    public string LatestLogFile => Path.Combine(LogsDir, "latest.log");

    public Visibility ActionPanelVisibility => MainTab.GetSelectedTabInGroup("MainTabs")?.DataContext is Repo ? Visibility.Visible : Visibility.Collapsed;

    public static App Instance => (App)Current;
    public static string ExeDir => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _watcherTimer = new(state =>
        {
            SelectedRepoStateChanged?.Invoke();
        });

        AppSettings = new Settings();
        AppSettings.InstallDirChanged += () =>
        {
            if (!Directory.Exists(AppSettings.InstallDir))
                return;
            
            _watcher = new(AppSettings.InstallDir);
            _watcher.Changed += (_, args) =>
            {
                if (args.FullPath != LatestLogFile)
                    _watcherTimer.Change(100, Timeout.Infinite);
            };
            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
            
            LogWatcher = new(LatestLogFile);
            OnPropertyChanged(nameof(LogWatcher));
            
            MinecraftLauncherParameters parameters = MinecraftLauncherParameters.CreateDefault(new MinecraftPath(MinecraftBaseDir));
            Cml = new(parameters);
            
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
                
                if (Repos.TryGetValue(item, out var repo))
                    SelectedRepo = repo;
                
                OnPropertyChanged(nameof(ActionPanelVisibility));
            }
        };
    }

    private ICommand _loginCommand;
    public ICommand LoginCommand => _loginCommand ??= new RelayCommand(async () =>
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

        if (MainTab.GetGroupSelection("MainTabs") == "account")
        {
            MainTab.SelectTabByItemName("MainTabs", Repos.Keys.FirstOrDefault());
        }
    });

    private void RepoStateChanged(Repo repo)
    {
        if (repo.CurrentState == Repo.RepoStateType.Running)
            RunningRepo = repo;
        else if (repo == RunningRepo)
            RunningRepo = null;
        
        if (repo == SelectedRepo)
            SelectedRepoStateChanged?.Invoke();
    }

    private async Task FetchRepos()
    {
        HttpClient client = new();
        var response = await client.GetAsync("https://raw.githubusercontent.com/TwilightMage/MineLauncherMaster/refs/heads/main/repos.json");
        
        if (!response.IsSuccessStatusCode)
            return;
        
        var json = await response.Content.ReadAsStringAsync();
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new VersionConverter() }
        };


        if (System.Text.Json.Nodes.JsonNode.Parse(json) is { } jsonNode)
        {
            foreach (var child in jsonNode.AsObject())
            {
                RepoInfo info = child.Value.Deserialize<RepoInfo>(options);
                info.Key = child.Key;
                var repo = new Repo(info);
                Repos.Add(child.Key, repo);

                repo.CurrentStateChanged += RepoStateChanged;
                repo.FetchVersion();
            }
        }

        if (Repos.Count > 0)
        {
            if (Repos.TryGetValue(AppSettings.Repo ?? "", out var foundRepo))
                SelectedRepo = foundRepo;
            else
            {
                SelectedRepo = Repos.FirstOrDefault().Value;
                AppSettings.Repo = SelectedRepo.Info.Key;
            }
            
            MainTab.SelectTabByItemName("MainTabs", SelectedRepo.Info.Key);
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