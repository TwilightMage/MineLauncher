using System;
using HelixToolkit.Wpf.SharpDX;

namespace MineLauncher.BlockBench;

public class Texture : IProjectNode
{
    public string Name { get; set; }
    public Guid Uuid { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public TextureModel Source { get; set; }
}