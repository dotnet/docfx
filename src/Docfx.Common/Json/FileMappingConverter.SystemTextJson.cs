// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace Docfx;

internal partial class FileMappingConverter
{
    internal class SystemTextJsonConverter : JsonConverter<FileMapping>
    {
        public override FileMapping? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                    var item = JsonSerializer.Deserialize<FileMappingItem>(ref reader, options);
                    return new FileMapping(item);

                // Array form
                case JsonTokenType.StartArray:
                    var items = JsonSerializer.Deserialize<FileMappingItem[]>(ref reader, options);
                    return new FileMapping(items);

                default:
                    throw new System.Text.Json.JsonException($"Unsupported token type({reader.TokenType}).");
            }
        }

        public override void Write(Utf8JsonWriter writer, FileMapping value, JsonSerializerOptions options)
        {
            var items = value.Items;
            JsonSerializer.Serialize(writer, items, options);
        }
    }
}
