using System;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;

namespace MineLauncher.Loaders;

public class VanillaLoader : LoaderBase
{
    public override async Task Install(MinecraftLauncher cml, Version version, Action<ulong, ulong> progressCallback,
        CancellationTokenSource cts)
    {
        await cml.InstallAsync(version.ToString(), null,
            new Progress<ByteProgress>(progress =>
            {
                progressCallback?.Invoke((ulong)progress.ProgressedBytes, (ulong)progress.TotalBytes);
            }), cts.Token);
    }

    public override string GameVersion(Version mcVersion, Version loaderVersion) => mcVersion.ToString();
}