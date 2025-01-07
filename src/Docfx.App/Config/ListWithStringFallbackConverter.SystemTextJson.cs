// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Docfx;


internal partial class ListWithStringFallbackConverter
{
    /// <summary>
    /// JsonConverter for <see cref="ListWithStringFallback"/>.
    /// </summary>
    internal class SystemTextJsonConverter : JsonConverter<ListWithStringFallback>
    {
        public override ListWithStringFallback Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var tokenType = reader.TokenType;
            switch (tokenType)
            {
                case JsonTokenType.String:
                    {
                        var value = reader.GetString();
                        return new ListWithStringFallback([value]);
                    }
                case JsonTokenType.StartArray:
                    {
                        var items = JsonSerializer.Deserialize<string[]>(ref reader, options);
                        return new ListWithStringFallback(items);
                    }
                case JsonTokenType.StartObject:
                    {
                        using var document = JsonDocument.ParseValue(ref reader);
                        JsonElement root = document.RootElement;
                        var values = root.EnumerateObject().Select(x => x.ToString());
                        return new ListWithStringFallback(values);
                    }
                default:
                    throw new JsonException($"TokenType({reader.TokenType}) is not supported.");
            }
        }

        public override void Write(Utf8JsonWriter writer, ListWithStringFallback value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                writer.WriteStringValue(item);
            }
            writer.WriteEndArray();
        }
    }
}
