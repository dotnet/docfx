// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class FileMetadataPairsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FileMetadataPairs);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = reader.Value;
            IEnumerable<JToken> jItems;
            if (reader.TokenType == JsonToken.StartObject)
            {
                jItems = JContainer.Load(reader);
            }
            else throw new JsonReaderException($"{reader.TokenType.ToString()} is not a valid {objectType.Name}.");
            return new FileMetadataPairs(jItems.Select(s => ParseItem(s)).ToList());
        }

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
