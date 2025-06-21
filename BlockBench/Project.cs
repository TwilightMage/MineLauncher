using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MineLauncher.BlockBench;

public class Project
{
    public string Name { get; set; }
    
    [JsonIgnore]
    public List<Element> Elements { get; set; }
    
    [JsonIgnore]
    public List<IProjectNode> Outliner { get; set; }
    
    [JsonIgnore]
    public Dictionary<int, Texture> Textures { get; set; }
    
    public readonly Dictionary<Guid, IProjectNode> NodeCache = new();
    
    public static Project Load(string path) => Load(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
    
    public static Project Load(Stream stream, LoadConfig config = null)
    {
        if (JsonNode.Parse(stream)?.AsObject() is { } obj)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new Vector3Converter(),
                    new RectConverter(),
                    new TextureModelConverter(),
                    new JsonStringEnumConverter(),
                }
            };
            
            var bb = obj.Deserialize<Project>(options);
            
            bb.Elements = new();
            foreach (var entry in obj["elements"].AsArray())
            {
                if (entry["type"].GetValue<string>().ToLower() == "cube")
                    bb.Elements.Add(entry.Deserialize<CubeElement>(options));
                else
                    bb.Elements.Add(null); // unsupported (yet) element type "mesh"
            }

            bb.Outliner = new();
            foreach (var entry in obj["outliner"].AsArray())
            {
                if (entry is JsonObject group)
                {
                    bb.Outliner.Add(group.Deserialize<Group>(options));
                }
                else
                {
                    bb.Outliner.Add(bb.Elements.First(elem => elem.Uuid.ToString() == entry.GetValue<string>()));
                }
            }
            
            bb.Textures = new();
            if (config?.TextureOverrides != null)
            {
                foreach (var ovr in config.TextureOverrides)
                {
                    if (ovr.Value is null)
                        continue;
                    
                    var info = ovr.Value.TextureInfoLoader.Load(ovr.Value.Guid);
                    bb.Textures.Add(ovr.Key, new Texture
                    {
                        Name = $"{ovr.Key} Override",
                        Uuid = ovr.Value.Guid,
                        Source = ovr.Value,
                        Width = info.Width,
                        Height = info.Height,
                    });
                }
            }
            foreach (var entry in obj["textures"].AsArray())
            {
                int id = int.Parse(entry["id"].GetValue<string>());
                if (!bb.Textures.ContainsKey(id))
                    bb.Textures.Add(id, entry.Deserialize<Texture>(options));
            }
            
            bb.Recache();
            
            return bb;
        }

        return null;
    }

    public void Recache()
    {
        NodeCache.Clear();
        foreach (var element in Elements)
        {
            NodeCache[element.Uuid] = element;
        }
        foreach (var group in Outliner)
        {
            NodeCache[group.Uuid] = group;
        }
        foreach (var texture in Textures)
        {
            NodeCache[texture.Value.Uuid] = texture.Value;
        }
    }
}