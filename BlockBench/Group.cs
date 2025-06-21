using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace MineLauncher.BlockBench;

public class Group : IProjectNode
{
    public string Name { get; set; }
    public Vector3 Origin { get; set; }
    public Vector3 Rotation { get; set; }
    public Guid Uuid { get; set; }
    public List<Guid> Children { get; set; }
        
    public IEnumerable<IProjectNode> GetChildren(Project bb) => Children.Select(guid => bb.NodeCache.TryGetValue(guid, out var child) ? child : null);
}