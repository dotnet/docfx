// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace Docfx;

internal partial class FileMappingConverter
{
    internal class SystemTextJsonConverter : JsonConverter<FileMapping>
    {
        public override FileMapping Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                // Compact Form
                case JsonTokenType.String:
                    return new FileMapping(new FileMappingItem
                    {
                        Files = new FileItems(reader.GetString()),
                    });
                // Object form
                case JsonTokenType.StartObject:
                    return new FileMapping(JsonSerializer.Deserialize<FileMappingItem>(ref reader, options));

                // Array form
                case JsonTokenType.StartArray:
                    var items = new List<FileMappingItem>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.String:
                                items.Add(new FileMappingItem
                                {
                                    Files = new FileItems(reader.GetString()),
                                });
                                break;
                            case JsonTokenType.StartObject:
                                items.Add(JsonSerializer.Deserialize<FileMappingItem>(ref reader, options)!);
                                break;
                            default:
                                throw new JsonException($"Unsupported token type({reader.TokenType}).");
                        }
                    }
                    return new FileMapping(items);

                default:
                    throw new JsonException($"Unsupported token type({reader.TokenType}).");
            }
        }

        public override void Write(Utf8JsonWriter writer, FileMapping value, JsonSerializerOptions options)
        {
            var items = value.Items;
            JsonSerializer.Serialize(writer, items, options);
        }
    }
}
