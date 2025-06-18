using System;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;

namespace MineLauncher.Loaders;

public class ForgeLoader : LoaderBase
{
    public override Task Install(MinecraftLauncher cml, Version version,
        Action<ulong, ulong> progressCallback, CancellationTokenSource cts)
    {
        ForgeInstaller installer = new(cml);
        return installer.Install(version.ToString(), new ForgeInstallOptions
        {
            CancellationToken = cts.Token,
            ByteProgress = new Progress<ByteProgress>(progress =>
            {
                progressCallback?.Invoke((ulong)progress.ProgressedBytes, (ulong)progress.TotalBytes);
            })
        });
    }

    public override string GameVersion(Version mcVersion, Version loaderVersion) => $"{mcVersion}-forge-{loaderVersion}";
}