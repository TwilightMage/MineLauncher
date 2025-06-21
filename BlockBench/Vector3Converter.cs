using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpDX;

namespace MineLauncher.BlockBench;

public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
        float z = reader.GetSingle();

        reader.Read(); // Read the end array token
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Expected end of array");
        }

        return new Vector3(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}