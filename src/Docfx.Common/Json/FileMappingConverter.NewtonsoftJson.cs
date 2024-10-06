// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx;

internal partial class FileMappingConverter
{
    internal class NewtonsoftJsonConverter : JsonConverter
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
