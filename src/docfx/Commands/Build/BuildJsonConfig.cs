// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Glob;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class BuildJsonConfig
    {
        [JsonIgnore]
        public string BaseDirectory { get; set; }

        [JsonIgnore]
        public string OutputFolder { get; set; }

        [JsonProperty("force")]
        public bool Force { get; set; }

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

        /// <summary>
        /// Metadata that applies to some specific files.
        /// The key is the metadata name.
        /// For each item of the value:
        ///     The key is the glob pattern to match the files.
        ///     The value is the value of the metadata.
        /// </summary>
        [JsonProperty("fileMetadata")]
        public Dictionary<string, FileMetadataPairs> FileMetadata { get; set; }

        [JsonProperty("template")]
        public ListWithStringFallback Templates { get; set; } = new ListWithStringFallback();

        [JsonProperty("theme")]
        public ListWithStringFallback Themes { get; set; }

        [JsonProperty("serve")]
        public bool Serve { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }
    }

    [JsonConverter(typeof(FileMetadataPairsConverter))]
    public class FileMetadataPairs
    {
        // Order matters, the latter one overrides the former one
        private List<FileMetadataPairsItem> _items;
        public IReadOnlyList<FileMetadataPairsItem> Items
        {
            get
            {
                return _items.AsReadOnly();
            }
        }

        public FileMetadataPairs(List<FileMetadataPairsItem> items)
        {
            _items = items;
        }

        public FileMetadataPairs(FileMetadataPairsItem item)
        {
            _items = new List<FileMetadataPairsItem>() { item };
        }

        public FileMetadataPairsItem this[int index]
        {
            get
            {
                return _items[index];
            }
        }

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }
    }

    public class FileMetadataPairsItem
    {
        public GlobMatcher Glob { get; }

        /// <summary>
        /// JObject, no need to transform it to object as the metadata value will not be used but only to be serialized
        /// </summary>
        public object Value { get; }
        public FileMetadataPairsItem(string pattern, object value)
        {
            Glob = new GlobMatcher(pattern);
            Value = value;
        }
    }

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
