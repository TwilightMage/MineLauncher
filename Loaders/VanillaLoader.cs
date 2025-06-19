using System;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Installers;

namespace MineLauncher.Loaders;

public class VanillaLoader : LoaderBase
{
    public override async Task Install(MinecraftLauncher cml, Version version, Version loaderVersion,
        Action<ulong, ulong> progressCallback,
        CancellationTokenSource cts)
    {
        try
        {
            await cml.InstallAsync(version.ToString(),
                new Progress<InstallerProgressChangedEventArgs>(args =>
                {
                    switch (args.EventType)
                    {
                        case InstallerEventType.Queued:
                            Console.WriteLine($"Vanilla >>> Queued file {args.Name}, total queued {args.TotalTasks}");
                            break;
                        case InstallerEventType.Done:
                            Console.WriteLine($"Vanilla >>> Done file {args.Name}, progress {args.ProgressedTasks} of {args.TotalTasks}");
                            break;
                    }
                }),
                new Progress<ByteProgress>(progress =>
                {
                    progressCallback?.Invoke((ulong)progress.ProgressedBytes, (ulong)progress.TotalBytes);
                }), cts.Token);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    public override string GameVersion(Version mcVersion, Version loaderVersion) => mcVersion.ToString();
}