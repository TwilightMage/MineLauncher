using System;
using System.IO;
using System.Threading.Tasks;
using CmlLib.Core;

namespace MineLauncher.Loaders;

public abstract class LoaderBase
{
    public abstract Task Install(MinecraftLauncher cml, Version version, Action<ulong, ulong> progressCallback);
    public abstract string GameVersion(Version mcVersion, Version loaderVersion);
}