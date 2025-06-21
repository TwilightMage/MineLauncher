using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace MineLauncher.BlockBench;

public class RectConverter : JsonConverter<Rect>
{
    public override Rect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array");
        }

        reader.Read();
        float x = reader.GetSingle();
    
        reader.Read();
        float y = reader.GetSingle();
    
        reader.Read();
        float w = reader.GetSingle();
            
        reader.Read();
        float h = reader.GetSingle();
    
        reader.Read(); // Read the end array token
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Expected end of array");
        }

        return new Rect(x, y, w, h);
    }

    public override void Write(Utf8JsonWriter writer, Rect value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Width);
        writer.WriteNumberValue(value.Height);
        writer.WriteEndArray();
    }
}