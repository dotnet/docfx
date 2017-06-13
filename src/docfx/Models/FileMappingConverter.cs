// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class FileMappingConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FileMapping);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var model = new FileMapping();
            var value = reader.Value;
            IEnumerable<JToken> jItems;
            if (reader.TokenType == JsonToken.StartArray)
            {
                jItems = JArray.Load(reader);
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
                model.Add(FileModelParser.ParseItem(jItems.ToString()));
            }
            else if (jItems is JObject)
            {
                model.Add(FileModelParser.ParseItem((JToken)jItems));
            }
            else
            {
                foreach (var item in jItems)
                {
                    FileMappingItem itemModel = FileModelParser.ParseItem(item);
                    model.Add(itemModel);
                }
            }

            return model;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((FileMapping)value).Items);
        }
    }
}
