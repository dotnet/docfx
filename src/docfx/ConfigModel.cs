// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    
    class ConfigModel
    {
        [JsonProperty("projects")]
        public FileMapping Projects { get; set; }

        [JsonProperty("conceptuals")]
        public FileMapping Conceptuals { get; set; }

        [JsonProperty("externalReferences")]
        public FileMapping ExternalReferences { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("template")]
        public string TemplateFolder { get; set; }

        [JsonProperty("theme")]
        public string TemplateTheme { get; set; }

        [JsonProperty("output")]
        public string OutputFolder { get; set; }

        /// <summary>
        /// DO NOT add --raw option to xdoc.json config
        /// </summary>
        [JsonIgnore]
        public bool PreserveRawInlineComments { get; set; }

        /// <summary>
        /// The directory of the xdoc.json to do glob search, if there is no xdoc.json file, use current folder
        /// </summary>
        [JsonIgnore]
        public string BaseDirectory { get; set; }
    }


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
            else if (reader.TokenType == JsonToken.StartObject)
            {
                jItems = JContainer.Load(reader);
            }
            else
            {
                jItems = JObject.Load(reader);
            }

            foreach (var item in jItems)
            {
                FileMappingItem itemModel = FileModelParser.ParseItem(item);
                model.Add(itemModel);
            }

            return model;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((FileMapping)value).Items);
        }
    }

    [JsonConverter(typeof(FileMappingConverter))]
    class FileMapping
    {
        private List<FileMappingItem> _items = new List<FileMappingItem>();

        public IReadOnlyList<FileMappingItem> Items
        {
            get { return _items.AsReadOnly(); }
        }

        public FileMapping() : base() { }

        public FileMapping(IEnumerable<FileMappingItem> items)
        {
            foreach (var item in items) this.Add(item);
        }
        public FileMapping(FileMappingItem item)
        {
            this.Add(item);
        }

        public FileMappingItem this[int i]
        {
            get
            {
                return _items[i];
            }
        }

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }

        /// <summary>
        /// Should not merge FileMappingItems even if they are using the same name, because other propertes also matters, e.g. cwd, exclude.
        /// </summary>
        /// <param name="item"></param>
        public void Add(FileMappingItem item)
        {
            if (item == null || item.Files == null || item.Files.Count == 0) return;

            _items.Add(item);
        }
    }

    class FileMappingItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("files")]
        public FileItems Files { get; set; }

        [JsonProperty("exclude")]
        public FileItems Exclude { get; set; }

        [JsonProperty("cwd")]
        public string CurrentWorkingDirectory { get; set; }
    }

    class FileItems : List<string>
    {
        public FileItems(string file) : base()
        {
            this.Add(file);
        }

        public FileItems(IEnumerable<string> files) : base(files)
        {
        }

        public static explicit operator FileItems(string input)
        {
            return new FileItems(input);
        }
    }

    enum FileMappingFormat
    {
        /// <summary>
        /// This format supports multiple name-files file mappings, with the property name as the name, and the value as the files.
        /// </summary>
        /// <example>
        /// projects: {
        ///  "name1": ["file1", "file2"],
        ///  "name2": "file3"
        /// }
        /// </example>
        ObjectFormat,

        /// <summary>
        /// This form supports multiple name-files file mappings, and also allows additional properties per mapping.
        /// </summary>
        /// <example>
        /// projects: [
        ///  {name: "name1", files: ["file1", "file2"]},
        ///  {name: "name2", files: "file3"},
        ///  {files:  ["file4", "file5"], exclude: ["file5"]}
        ///]
        /// </example>
        ArrayFormat,

        /// <summary>
        /// This form supports multiple file patterns in an array
        /// </summary>
        /// <example>
        /// projects: ["file1", "file2"]
        /// </example>
        CompactFormat,
    }

    class FileModelParser
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="JsonReaderException"></exception>
        public static FileMapping Parse(string input, string key)
        {
            FileMapping model = new FileMapping();

            var value = JObject.Parse(input)[key];
            foreach (var item in value)
            {
                var itemModel = ParseItem(item);
                model.Add(itemModel);
            }

            return model;
        }

        public static FileMappingItem ParseItem(JToken item)
        {
            if (item.Type == JTokenType.Object)
            {
                return JsonConvert.DeserializeObject<FileMappingItem>(item.ToString());
            }
            else if (item.Type == JTokenType.Property)
            {
                JProperty jProperty = item as JProperty;
                FileMappingItem model = new FileMappingItem { Name = jProperty.Name };
                var value = jProperty.Value;
                if (value.Type == JTokenType.Array)
                {
                    model.Files = new FileItems(value.Select(s => s.Value<string>()));
                }
                else if (value.Type == JTokenType.String)
                {
                    model.Files = new FileItems((string)value);
                }
                else
                {
                    throw new JsonReaderException(string.Format("Unsupported value {0} (type: {1}).", value, value.Type));
                }

                return model;
            }
            else if (item.Type == JTokenType.String)
            {
                return new FileMappingItem { Files = new FileItems(item.Value<string>()) };
            }
            else
            {
                throw new JsonReaderException(string.Format("Unsupported value {0} (type: {1}).", item, item.Type));
            }
        }
    }
}
