// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class MergeJsonConfigConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MergeJsonConfig);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var model = new MergeJsonConfig();
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

            if (jItems is JValue one)
            {
                model.Add(serializer.Deserialize<MergeJsonItemConfig>(one.CreateReader()));
            }
            else if (jItems is JObject)
            {
                model.Add(serializer.Deserialize<MergeJsonItemConfig>(((JToken)jItems).CreateReader()));
            }
            else
            {
                foreach (var item in jItems)
                {
                    MergeJsonItemConfig itemModel = serializer.Deserialize<MergeJsonItemConfig>(item.CreateReader());
                    model.Add(itemModel);
                }
            }

            return model;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((MergeJsonConfig)value).ToArray());
        }
    }
}
