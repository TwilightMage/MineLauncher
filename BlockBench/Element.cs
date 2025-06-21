using System;
using SharpDX;

namespace MineLauncher.BlockBench;

public abstract class Element : IProjectNode
{
    public string Name { get; set; }
    public Vector3 Origin { get; set; }
    public Vector3 Rotation { get; set; }
    public Guid Uuid { get; set; }
}

public class CubeElement : Element
{
    public Vector3 From { get; set; }
    public Vector3 To { get; set; }
    public CubeFaceGroup Faces { get; set; }
    public float Inflate { get; set; } = 0;
        
    public Vector3 ActualFrom => From - new Vector3(Inflate, Inflate, Inflate);
    public Vector3 ActualTo => To + new Vector3(Inflate, Inflate, Inflate);
}