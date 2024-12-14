// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Docfx;

internal partial class MergeJsonConfigConverter
{
    /// <summary>
    /// JsonConverter for <see cref="MergeJsonConfig"/>.
    /// </summary>
    internal class SystemTextJsonConverter : JsonConverter<MergeJsonConfig>
    {
        public override MergeJsonConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var model = new MergeJsonConfig();

            var tokenType = reader.TokenType;

            switch (tokenType)
            {
                case JsonTokenType.StartArray:
                    {
                        var items = JsonSerializer.Deserialize<MergeJsonItemConfig[]>(ref reader, options);
                        return new MergeJsonConfig(items);
                    }
                case JsonTokenType.StartObject:
                    {
                        var item = JsonSerializer.Deserialize<MergeJsonItemConfig>(ref reader, options);
                        return new MergeJsonConfig(item);
                    }
                default:
                    throw new JsonException($"TokenType({tokenType}) is not supported.");
            }
        }

        public override void Write(Utf8JsonWriter writer, MergeJsonConfig value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }
}
