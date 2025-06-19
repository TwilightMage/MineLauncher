using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installers;

namespace MineLauncher.Loaders;

public class ForgeLoader : LoaderBase
{
    public override Task Install(MinecraftLauncher cml, Version version, Version loaderVersion,
        Action<ulong, ulong> progressCallback,
        CancellationTokenSource cts)
    {
        try
        {
            DisableForgeAd();
        
            ForgeInstaller installer = new(cml);
            return installer.Install(version.ToString(), loaderVersion.ToString(), new ForgeInstallOptions
            {
                CancellationToken = cts.Token,
                FileProgress = new Progress<InstallerProgressChangedEventArgs>(args =>
                {
                    switch (args.EventType)
                    {
                        case InstallerEventType.Queued:
                            Console.WriteLine($"Forge >>> Queued file {args.Name}, total queued {args.TotalTasks}");
                            break;
                        case InstallerEventType.Done:
                            Console.WriteLine($"Forge >>> Done file {args.Name}, progress {args.ProgressedTasks} of {args.TotalTasks}");
                            break;
                    }
                }),
                InstallerOutput = new Progress<string>(s =>
                {
                    Console.WriteLine($"Forge >>> {s}");
                }),
                ByteProgress = new Progress<ByteProgress>(progress =>
                {
                    progressCallback?.Invoke((ulong)progress.ProgressedBytes, (ulong)progress.TotalBytes);
                })
            });
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return Task.CompletedTask;
        }
    }

    private void DisableForgeAd()
    {
        string dummyPath = Path.Combine(App.ExeDir, "dummy.exe");
        
        var adUrlField = typeof(ForgeInstaller).GetField("ForgeAdUrl");
        adUrlField?.SetValue(null, dummyPath);
    }

    public override string GameVersion(Version mcVersion, Version loaderVersion) => $"{mcVersion}-forge-{loaderVersion}";
}