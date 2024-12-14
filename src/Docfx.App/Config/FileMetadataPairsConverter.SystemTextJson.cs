// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Docfx.Common;

#nullable enable

namespace Docfx;

internal partial class FileMetadataPairsConverter
{
    /// <summary>
    /// JsonConverter for FileMetadataPairs
    /// </summary>
    internal class SystemTextJsonConverter : JsonConverter<FileMetadataPairs>
    {
        public override FileMetadataPairs Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"{reader.TokenType} is not a valid {typeToConvert.Name}.");
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var properties = document.RootElement.EnumerateObject();
            var items = properties.Select(x => new FileMetadataPairsItem(x.Name, ToInferredType(x.Value))).ToArray();
            return new FileMetadataPairs(items);
        }

        public override void Write(Utf8JsonWriter writer, FileMetadataPairs value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var item in value.Items)
            {
                writer.WritePropertyName(item.Glob.Raw);
                writer.WriteRawValue(JsonUtility.Serialize(item.Value));
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Convert JsonElement to .NET object.
        /// </summary>
        private static object? ToInferredType(JsonElement elem)
        {
            switch (elem.ValueKind)
            {
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.String when elem.TryGetDateTime(out DateTime datetime):
                    return datetime;
                case JsonValueKind.String:
                    return elem.GetString();
                case JsonValueKind.Array:
                    return elem.EnumerateArray().Select(ToInferredType).ToArray();
                case JsonValueKind.Object:
                    var properties = elem.EnumerateObject();
                    return properties.ToDictionary(x => x.Name, x => ToInferredType(x.Value));
                case JsonValueKind.Number when elem.TryGetInt32(out int intValue):
                    return intValue;
                case JsonValueKind.Number when elem.TryGetInt64(out long longValue):
                    return longValue;
                case JsonValueKind.Number:
                    return elem.GetDouble();
                case JsonValueKind.Undefined:
                default:
                    throw new JsonException($"JsonValueKind({elem.ValueKind}) is not supported.");
            }
        }
    }
}
