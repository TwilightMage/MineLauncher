using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using LibGit2Sharp;
using Version = System.Version;

namespace MineLauncher;

public class Repo : INotifyPropertyChanged
{
    
    public enum RepoTaskType
    {
        None,
        Fetching,
        Updating,
        Running,
    }

    public Repo()
    {
        App.Instance.AppSettings.LanguageChanged += () => OnPropertyChanged(nameof(Title));
    }
        
    public string Key { get; set; }

    public string Title
    {
        get
        {
            var lang = App.Instance.AppSettings.GetUsedLanguage();
            if (TitleByLanguage.TryGetValue(lang, out var title))
                return title;
            return TitleByLanguage[Language.English];
        }
    }
    public Dictionary<Language, string> TitleByLanguage;
    
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
                CurrentTaskChanged?.Invoke(this);
            }
        }
    }
    public event Action<Repo> CurrentTaskChanged;

    public bool IsLoaderInstalled => Directory.Exists(Path.Combine(App.Instance.MinecraftBaseDir, "versions", GameVersion));
    public bool IsModPackUpToDate => GitRepo != null && GitRepo.Head.Tip.Sha == GitRepo.Branches["main"].Tip.Sha;
    public bool IsUpToDate => IsLoaderInstalled && IsModPackUpToDate;
        
    public string RepoUpdateError;
        
    private float _repoUpdateProgress;
    public float RepoUpdateProgress
    {
        get => _repoUpdateProgress;
        private set => SetField(ref _repoUpdateProgress, value);
    }

    private string _repoUpdateProgressText = "";
    public string RepoUpdateProgressText
    {
        get => _repoUpdateProgressText;
        private set
        {
            if (SetField(ref _repoUpdateProgressText, value))
            {
                OnPropertyChanged(nameof(RepoUpdateProgressTextVisibility));
            }
        }
    }

    public Visibility RepoUpdateProgressTextVisibility => string.IsNullOrEmpty(RepoUpdateProgressText)
        ? Visibility.Collapsed
        : Visibility.Visible;
        
    public bool Running { get; set; }
        
    public string RepoDir => Path.Combine(App.Instance.AppSettings.InstallDir, Key);
        
    public string ModpackDir => Path.Combine(RepoDir, ".minecraft");
    public string ModsDir => Path.Combine(ModpackDir, "mods");
    public string ConfigsDir => Path.Combine(ModpackDir, "config");
    public string ResourcePacksDir => Path.Combine(ModpackDir, "resourcepacks");
    public string ShaderPacksDir => Path.Combine(ModpackDir, "shaderpacks");
    public string ServersFile => Path.Combine(ModpackDir, "servers.dat");
    
    private Process _runProcess;
    private CancellationTokenSource _cts;

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
            LibGit2Sharp.Commands.Fetch(GitRepo, remote.Name, refSpecs, options, msg);

            if (IsUpToDate)
            {
                UpdateProgress(100);
            }
        }
        catch (Exception)
        {
            // ignored
        }

        CurrentTask = RepoTaskType.None;
    }

    public async Task Update()
    {
        if (string.IsNullOrEmpty(App.Instance.AppSettings.InstallDir) || string.IsNullOrEmpty(RepoUrl))
            return;
            
        if (CurrentTask != RepoTaskType.None)
            return;
        CurrentTask = RepoTaskType.Updating;

        _cts = new CancellationTokenSource();
            
        // Update minecraft
        if (!IsLoaderInstalled)
        {
            UpdateProgress(0, Properties.Strings.Progress_Preparing);
            
            if (Loaders.Manager.GetLoader(Loader) is { } loader)
            {
                await loader.Install(App.Instance.Cml, MCVersion, (progressedBytes, totalBytes) =>
                {
                    UpdateProgress((float)progressedBytes / totalBytes * 100, Properties.Strings.Progress_InstallingMinecraft.FormatInline(Utils.FormatSize(progressedBytes), Utils.FormatSize(totalBytes)));
                }, _cts);
            }
            else
            {
                UpdateError(Properties.Strings.Progress_Error_Loader);
                    
                CurrentTask = RepoTaskType.None;
                return;
            }
        }

        if (_cts.IsCancellationRequested)
        {
            UpdateProgress(100);
            CurrentTask = RepoTaskType.None;
            return;
        }
        
        // Update modpack
        if (!IsModPackUpToDate)
        {
            UpdateProgress(0, Properties.Strings.Progress_Preparing);
            
            if (GitRepo == null)
            {
                await Task.Run(() =>
                {
                    Repository.Clone(RepoUrl, RepoDir, new CloneOptions
                    {
                        OnCheckoutProgress = (_, steps, totalSteps) =>
                        {
                            UpdateProgress((float)steps / totalSteps * 100, Properties.Strings.Progress_InstallingModpack.FormatInline(steps, totalSteps));
                        }
                    });
                });
            
                GitRepo = new Repository(RepoDir);
            }
            else
            {
                LibGit2Sharp.Commands.Checkout(GitRepo, GitRepo.Branches["main"]);

                if (!IsModPackUpToDate)
                {
                    var signature = new Signature("MineLauncher", "<EMAIL>", DateTimeOffset.Now);
                    GitRepo.MergeFetchedRefs(signature, new MergeOptions
                    {
                        FastForwardStrategy = FastForwardStrategy.FastForwardOnly,
                        FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                        OnCheckoutProgress = (_, steps, totalSteps) =>
                        {
                            UpdateProgress((float)steps / totalSteps * 100, Properties.Strings.Progress_UpdatingModpack.FormatInline(steps, totalSteps));
                        }
                    });
                }
            }
        }
        
        App.Instance.Dispatcher.Invoke(() =>
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

        try
        {
            _cts = new CancellationTokenSource();
            
            _runProcess = await App.Instance.Cml.CreateProcessAsync(GameVersion, new MLaunchOption
            {
                ArgumentDictionary = new Dictionary<string, string>
                {
                    ["game_directory"] = ModpackDir,
                },
                Session = MSession.CreateOfflineSession(App.Instance.Account.Username),
                JavaPath = string.IsNullOrEmpty(App.Instance.AppSettings.JavaPath) ? null : App.Instance.AppSettings.JavaPath,
                MinimumRamMb = App.Instance.AppSettings.MinJavaSizeMb,
                MaximumRamMb = App.Instance.AppSettings.MaxJavaSizeMb,
            });
            
            _runProcess.StartInfo.RedirectStandardOutput = true;
            _runProcess.StartInfo.RedirectStandardError = true;
            _runProcess.StartInfo.UseShellExecute = false;
            
            // Discard default output
            _runProcess.OutputDataReceived += (_, _) => { };
            _runProcess.ErrorDataReceived += (_, _) => { };
            
            if (_cts.IsCancellationRequested)
                return;
            
            using var watcher = new LogFileWatcher(App.Instance.LatestLogFile);
            watcher.LinePrint += Console.WriteLine;
            
            _runProcess.Start();

            _runProcess.BeginOutputReadLine();
            _runProcess.BeginErrorReadLine();
            
            var ctsTask = Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested && !_runProcess.HasExited)
                {
                    Thread.Sleep(100);
                }
                
                if (_runProcess is { HasExited: false })
                {
                    _runProcess.Kill();
                }
            });

            _runProcess.WaitForExit();
            
            // Cleanup
            await ctsTask;
            
            _cts = null;
            _runProcess = null;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
            
        CurrentTask = RepoTaskType.None;
    }

    public void CancelCurrentTask()
    {
        if (_cts != null)
        {
            _cts.Cancel();
        }
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