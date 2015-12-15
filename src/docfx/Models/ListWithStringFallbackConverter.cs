// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ListWithStringFallbackConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FileMapping);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var model = new ListWithStringFallback();
            var value = reader.Value;
            IEnumerable<JToken> jItems;
            if (reader.TokenType == JsonToken.StartArray)
            {
                jItems = JArray.Load(reader);
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                jItems = JContainer.Load(reader);
            }
            else if (reader.TokenType == JsonToken.String)
            {
                jItems = JRaw.Load(reader);
            }
            else
            {
                jItems = JObject.Load(reader);
            }

            if (jItems is JValue)
            {
                model.Add(jItems.ToString());
            }
            else
            {
                foreach (var item in jItems)
                {
                    model.Add(item.ToString());
                }
            }

            return model;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach(var item in (ListWithStringFallback)value)
            {
                serializer.Serialize(writer, item);
            }
            writer.WriteEndArray();
        }
    }
}
