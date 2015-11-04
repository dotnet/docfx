// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    public class BuildJsonConfig
    {
        [JsonIgnore]
        public string BaseDirectory { get; set; }

        [JsonProperty("content")]
        public FileMapping Content { get; set; }

        [JsonProperty("resource")]
        public FileMapping Resource { get; set; }

        [JsonProperty("overwrite")]
        public FileMapping Overwrite { get; set; }

        [JsonProperty("externalReference")]
        public FileMapping ExternalReference { get; set; }

        [JsonProperty("dest")]
        public string Destination { get; set; }

        [JsonProperty("globalMetadata")]
        public Dictionary<string, object> GlobalMetadata { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("template")]
        public ListWithStringFallback Templates { get; set; } = new ListWithStringFallback { Constants.DefaultTemplateName };

        [JsonProperty("theme")]
        public ListWithStringFallback Themes { get; set; }

        [JsonProperty("serve")]
        public bool Serve { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }
    }


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
            serializer.Serialize(writer, ((FileMapping)value).Items);
        }
    }

    [JsonConverter(typeof(ListWithStringFallbackConverter))]
    public class ListWithStringFallback : List<string>
    {
        public ListWithStringFallback() : base()
        {
        }

        public ListWithStringFallback(IEnumerable<string> list) : base(list)
        {
        }
    }
}
