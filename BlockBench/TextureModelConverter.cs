using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HelixToolkit.Wpf.SharpDX;

namespace MineLauncher.BlockBench;

public class TextureModelConverter : JsonConverter<TextureModel>
{
    public override TextureModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        byte[] bytes = Convert.FromBase64String(str.Substring(str.IndexOf(',') + 1));
        return TextureModel.Create(new MemoryStream(bytes));
    }

    public override void Write(Utf8JsonWriter writer, TextureModel value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}