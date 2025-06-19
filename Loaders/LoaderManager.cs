using System.Collections.Generic;

namespace MineLauncher.Loaders;

public static class LoaderManager
{
    private static Dictionary<string, LoaderBase> _loaders = new()
    {
        ["vanilla"] = new VanillaLoader(),
        ["forge"] = new ForgeLoader(),
    };
    
    public static LoaderBase GetLoader(string name)
    {
        return _loaders.TryGetValue(name, out var loader) ? loader : null;
    }
}