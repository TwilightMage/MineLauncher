using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using LibGit2Sharp;
using Version = System.Version;

namespace MineLauncher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public class Repo : INotifyPropertyChanged
    {
        public enum RepoTaskType
        {
            None,
            Fetching,
            Updating,
            Running,
        }
            
        public string Key { get; set; }
        public string Title { get; set; }
        public string Loader { get; set; }
        public Version MCVersion { get; set; }
        public Version LoaderVersion { get; set; }
        public string GameVersion { get; private set; }
        public string RepoUrl { get; set; }
        public string Changelog { get; set; }
        public Repository GitRepo { get; private set; }

        private RepoTaskType _currentTask = RepoTaskType.None;

        public RepoTaskType CurrentTask // do not change outside the active task
        {
            get => _currentTask;
            set
            {
                if (SetField(ref _currentTask, value))
                {
                    OnPropertyChanged(nameof(CanDispatchTask));
                }
            }
        }
        public bool CanDispatchTask => CurrentTask == RepoTaskType.None;

        public bool IsLoaderInstalled => Directory.Exists(Path.Combine(Instance.MinecraftBaseDir, "versions", GameVersion));
        public bool IsModPackUpToDate => GitRepo != null && GitRepo.Head.Tip.Sha == GitRepo.Branches["main"].Tip.Sha;
        public bool IsUpToDate => IsLoaderInstalled && IsModPackUpToDate;
            
        public string RepoUpdateError;
            
        private float _repoUpdateProgress = 0;
        public float RepoUpdateProgress
        {
            get => _repoUpdateProgress;
            set => SetField(ref _repoUpdateProgress, value);
        }

        private string _repoUpdateProgressText = "";
        public string RepoUpdateProgressText
        {
            get => _repoUpdateProgressText;
            set => SetField(ref _repoUpdateProgressText, value);
        }
            
        public bool Running { get; set; }
            
        public string RepoDir => Path.Combine(Instance.AppSettings.InstallDir, Key);
            
        public string ModpackDir => Path.Combine(RepoDir, ".minecraft");
        public string LogsDir => Path.Combine(ModpackDir, "logs");
        public string ModsDir => Path.Combine(ModpackDir, "mods");
        public string ConfigsDir => Path.Combine(ModpackDir, "config");
        public string ResourcePacksDir => Path.Combine(ModpackDir, "resourcepacks");
        public string ShaderPacksDir => Path.Combine(ModpackDir, "shaderpacks");
        public string ServersFile => Path.Combine(ModpackDir, "servers.dat");

        ~Repo()
        {
            if (GitRepo != null)
            {
                GitRepo.Dispose();
                GitRepo = null;
            }
        }

        public async Task FetchVersion()
        {
            if (CurrentTask != RepoTaskType.None)
                return;
            CurrentTask = RepoTaskType.Fetching;
                
            GameVersion = Loaders.Manager.GetLoader(Loader).GameVersion(MCVersion, LoaderVersion);
                
            GitRepo = null;
            try
            {
                GitRepo = new Repository(RepoDir); // Fails if repo dir not valid, e.g. not cloned yet
                    
                var options = new FetchOptions
                {
                    Prune = true,
                    TagFetchMode = TagFetchMode.Auto,
                };
                    
                GitRepo.Network.Remotes.Update("origin", updater => updater.Url = RepoUrl);

                var remote = GitRepo.Network.Remotes["origin"];
                var msg = "Fetching remote";
                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(GitRepo, remote.Name, refSpecs, options, msg);

                if (IsUpToDate)
                {
                    UpdateProgress(100);
                }
            }
            catch (Exception e)
            {
                // ignored
            }

            CurrentTask = RepoTaskType.None;
        }

        public async Task Update()
        {
            if (string.IsNullOrEmpty(Instance.AppSettings.InstallDir) || string.IsNullOrEmpty(RepoUrl))
                return;
                
            if (CurrentTask != RepoTaskType.None)
                return;
            CurrentTask = RepoTaskType.Updating;
            
            Current.Dispatcher.Invoke(() =>
            {
                UpdateProgress(0, "Preparing...");
            });
                
            // Update minecraft
            if (!IsLoaderInstalled)
            {
                if (Loaders.Manager.GetLoader(Loader) is { } loader)
                {
                    bool failed = false;
                    await loader.Install(Instance.Cml, MCVersion, (progressedBytes, totalBytes) =>
                    {
                        Current.Dispatcher.Invoke(() =>
                        {
                            UpdateProgress((float)progressedBytes / totalBytes * 100, $"Installing Minecraft... {Utils.FormatSize(progressedBytes)} / {Utils.FormatSize(totalBytes)}");
                        });
                    });
                    
                    if (failed)
                    {
                        CurrentTask = RepoTaskType.None;
                        return;
                    }
                }
                else
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        UpdateError("Failed to find loader");
                    });
                        
                    CurrentTask = RepoTaskType.None;
                    return;
                }
            }
            
            // Update modpack
            if (!IsModPackUpToDate)
            {
                if (GitRepo == null)
                {
                    Repository.Clone(RepoUrl, RepoDir, new CloneOptions
                    {
                        OnCheckoutProgress = (path, steps, totalSteps) =>
                        {
                            Current.Dispatcher.Invoke(() =>
                            {
                                RepoUpdateProgress = (float)steps / totalSteps * 100;
                                RepoUpdateProgressText = $"Updating modpack {steps}/{totalSteps}...";
                            });
                        }
                    });
                
                    Current.Dispatcher.Invoke(() =>
                    {
                        GitRepo = new Repository(RepoDir);
                    });
                }
                else
                {
                    Commands.Checkout(GitRepo, GitRepo.Branches["main"]);

                    if (!IsModPackUpToDate)
                    {
                        var signature = new Signature("MineLauncher", "<EMAIL>", DateTimeOffset.Now);
                        GitRepo.MergeFetchedRefs(signature, new MergeOptions
                        {
                            FastForwardStrategy = FastForwardStrategy.FastForwardOnly,
                            FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                            OnCheckoutProgress = (path, steps, totalSteps) =>
                            {
                                Current.Dispatcher.Invoke(() =>
                                {
                                    UpdateProgress((float)steps / totalSteps * 100, $"Updating modpack {steps}/{totalSteps}...");
                                });
                            }
                        });
                    }
                }
            }
            
            Current.Dispatcher.Invoke(() =>
            {
                UpdateProgress(100);
                OnPropertyChanged(nameof(IsUpToDate));
            });
                
            CurrentTask = RepoTaskType.None;
        }

        public async Task Run()
        {
            if (CurrentTask != RepoTaskType.None)
                return;
            CurrentTask = RepoTaskType.Running;

            var proc = await Instance.Cml.CreateProcessAsync(GameVersion, new MLaunchOption
            {
                ArgumentDictionary = new Dictionary<string, string>
                {
                    ["game_directory"] = ModpackDir,
                },
                Session = MSession.CreateOfflineSession(Instance.Account.Username),
                MinimumRamMb = Instance.AppSettings.MinJavaSizeMb,
                MaximumRamMb = Instance.AppSettings.MaxJavaSizeMb,
            });
                
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
                
            proc.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null) 
                    Console.WriteLine(args.Data);
            };

            proc.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null) 
                    Console.Error.WriteLine(args.Data);
            };
                
            proc.Start();
                
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
                
            proc.WaitForExit();
                
            CurrentTask = RepoTaskType.None;
        }

        public string GetHWID()
        {
            // windows key?
            return "";
        }

        private void UpdateProgress(float progress, string message = "")
        {
            RepoUpdateError = "";
            RepoUpdateProgress = progress;
            RepoUpdateProgressText = message;
        }
            
        private void UpdateError(string error)
        {
            RepoUpdateError = error;
            RepoUpdateProgress = 0;
            RepoUpdateProgressText = $"Error: {error}";
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

    public MinecraftLauncher Cml { get; private set; }
    
    public Account Account { get; private set; }

    public Settings AppSettings { get; private set; }

    public Dictionary<string, Repo> Repos { get; private set; } = new();
    public Repo SelectedRepo { get; set; }
        
    public Dictionary<string, string> Javas = new();
        
    public string MinecraftBaseDir => Path.Combine(Instance.AppSettings.InstallDir, "minecraft");
    public string LibrariesDir => Path.Combine(MinecraftBaseDir, "libraries");
    public string AssetsDir => Path.Combine(MinecraftBaseDir, "assets");
    public string LoaderDir(string loader) => Path.Combine(MinecraftBaseDir, loader);

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
            
        AppSettings.Load();
        
        Login();
            
        FetchRepos();
        FetchJavaVersions();

        MinecraftLauncherParameters parameters = MinecraftLauncherParameters.CreateDefault(new MinecraftPath(MinecraftBaseDir));
        Cml = new(parameters);
    }

    public void Login()
    {
        Account = new Account
        {
            Username = "Drakosha",
        };
    }

    public void FetchRepos()
    {
        Repos = new()
        {
            ["techno_magic"] = new()
            {
                Title = "Techno-Magic",
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
        
    private void FetchJavaVersions()
    {
        var PATH = Environment.GetEnvironmentVariable("PATH").Split(';');
        //try
        //{
        //    ProcessStartInfo psi = new ProcessStartInfo();
        //    psi.FileName = "java.exe";
        //    psi.Arguments = " -version";
        //    psi.RedirectStandardError= true;
        //    psi.UseShellExecute = false;
        //
        //    Process pr = Process.Start(psi);
        //    string strOutput = pr.StandardError.ReadLine().Split(' ')[2].Replace("\"", "");
        //
        //    Console.WriteLine(strOutput);
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Exception is " + ex.Message);
        //}

        Javas = new Dictionary<string, string>
        {
            {
                "jre1.8.0_451",
                "C:/Program Files/Java/jre1.8.0_451/bin/javaw.exe"
            }
        };
            
        if (!Javas.ContainsKey(AppSettings.Java))
            AppSettings.Java = Javas.Keys.FirstOrDefault();
    }
}