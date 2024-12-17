// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx;


internal partial class MergeJsonConfigConverter
{
    /// <summary>
    /// JsonConverter for <see cref="MergeJsonConfig"/>.
    /// </summary>
    internal class NewtonsoftJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MergeJsonConfig);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((MergeJsonConfig)value).ToArray());
        }
    }
}
