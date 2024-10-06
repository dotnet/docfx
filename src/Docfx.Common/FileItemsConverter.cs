// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace Docfx;

internal class FileItemsConverter : JsonConverter<FileItems>
{
    public override FileItems Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new FileItems(reader.GetString());
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var items = JsonSerializer.Deserialize<string[]>(ref reader, options);
            return new FileItems(items);
        }

        throw new JsonException("Expected string or array of strings.");
    }

    public override void Write(Utf8JsonWriter writer, FileItems value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }
}
