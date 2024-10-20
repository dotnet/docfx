// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx;

internal partial class FileMetadataPairsConverter
{
    /// <summary>
    /// JsonConverter for FileMetadataPairs
    /// </summary>
    internal class NewtonsoftJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FileMetadataPairs);
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IEnumerable<JToken> jItems;
            if (reader.TokenType == JsonToken.StartObject)
            {
                jItems = JContainer.Load(reader);
            }
            else throw new JsonReaderException($"{reader.TokenType} is not a valid {objectType.Name}.");
            return new FileMetadataPairs(jItems.Select(ParseItem).ToList());
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var item in ((FileMetadataPairs)value).Items)
            {
                writer.WritePropertyName(item.Glob.Raw);
                writer.WriteRawValue(JsonUtility.Serialize(item.Value));
            }
            writer.WriteEndObject();
        }

        private static FileMetadataPairsItem ParseItem(JToken item)
        {
            if (item.Type == JTokenType.Property)
            {
                JProperty jProperty = item as JProperty;
                var pattern = jProperty.Name;
                var rawValue = jProperty.Value;
                return new FileMetadataPairsItem(pattern, rawValue);
            }
            else
            {
                throw new JsonReaderException($"Unsupported value {item} (type: {item.Type}).");
            }
        }
    }
}
