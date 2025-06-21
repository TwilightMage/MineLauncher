using System.Collections.Generic;
using HelixToolkit.Wpf.SharpDX;
using SharpDX.Direct3D11;

namespace MineLauncher.BlockBench;

public class LoadConfig
{
    /**
     * Textures to override base textures.
     * DON'T PROVIDE SKINS HERE! Skins should be applied to the created material instances.
     */
    public Dictionary<int, TextureModel> TextureOverrides { get; set; }
}