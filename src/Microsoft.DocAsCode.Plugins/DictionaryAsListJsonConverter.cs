// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class DictionaryAsListJsonConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IList<KeyValuePair<string, T>>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = reader.Value;
            IEnumerable<JToken> jItems;
            if (reader.TokenType == JsonToken.StartObject)
            {
                jItems = JContainer.Load(reader);
            }
            else
            {
                throw new JsonReaderException($"{reader.TokenType.ToString()} is not a valid {objectType.Name}.");
            }

            return jItems.Select(s => ParseItem(s, serializer)).ToList();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var item in ((IList<KeyValuePair<string, T>>)value))
            {
                writer.WritePropertyName(item.Key);
                serializer.Serialize(writer, item.Value);
            }
            writer.WriteEndObject();
        }

        private KeyValuePair<string, T> ParseItem(JToken item, JsonSerializer serializer)
        {
            if (item.Type == JTokenType.Property)
            {
                JProperty jProperty = item as JProperty;
                var pattern = jProperty.Name;
                var value = jProperty.Value;
                return new KeyValuePair<string, T>(pattern, serializer.Deserialize<T>(value.CreateReader()));
            }
            else
            {
                throw new JsonReaderException($"Unsupported value {item} (type: {item.Type}).");
            }
        }
    }
}
