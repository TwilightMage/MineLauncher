using System;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;

namespace MineLauncher.Loaders;

public abstract class LoaderBase
{
    public abstract Task Install(MinecraftLauncher cml, Version version, Version loaderVersion,
        Action<ulong, ulong> progressCallback,
        CancellationTokenSource cts);
    public abstract string GameVersion(Version mcVersion, Version loaderVersion);
}