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
        public List<string> Templates { get; set; }

        [JsonProperty("theme")]
        public List<string> Themes { get; set; }

        [JsonProperty("output")]
        public string OutputFolder { get; set; }

        /// <summary>
        /// DO NOT add --raw option to docfx.json config
        /// </summary>
        [JsonIgnore]
        public bool PreserveRawInlineComments { get; set; }

        /// <summary>
        /// The directory of the docfx.json to do glob search, if there is no docfx.json file, use current folder
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

    [JsonConverter(typeof(FileMappingConverter))]
    public class FileMapping
    {
        private List<FileMappingItem> _items = new List<FileMappingItem>();

        public bool Expanded { get; set; }

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

    public class FileMappingItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("files")]
        public FileItems Files { get; set; }

        [JsonProperty("exclude")]
        public FileItems Exclude { get; set; }

        [JsonProperty("cwd")]
        public string CurrentWorkingDirectory { get; set; }

        /// <summary>
        /// Pattern match will be case sensitive.
        /// By default the pattern is case insensitive
        /// </summary>
        [JsonProperty("case")]
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// Disable pattern begin with `!` to mean negate
        /// By default the usage is enabled.
        /// </summary>
        [JsonProperty("noNegate")]
        public bool DisableNegate { get; set; }

        /// <summary>
        /// Disable `{a,b}c` => `["ac", "bc"]`.
        /// By default the usage is enabled.
        /// </summary>
        [JsonProperty("noExpand")]
        public bool DisableExpand { get; set; }

        /// <summary>
        /// Disable the usage of `\` to escape values.
        /// By default the usage is enabled.
        /// </summary>
        [JsonProperty("noEscape")]
        public bool DisableEscape { get; set; }

        /// <summary>
        /// Disable the usage of `**` to match everything including `/` when it is the beginning of the pattern or is after `/`.
        /// By default the usage is enable.
        /// </summary>
        [JsonProperty("noGlobStar")]
        public bool DisableGlobStar { get; set; }

        /// <summary>
        /// Allow files start with `.` to be matched even if `.` is not explicitly specified in the pattern.
        /// By default files start with `.` will not be matched by `*` unless the pattern starts with `.`.
        /// </summary>
        [JsonProperty("dot")]
        public bool AllowDotMatch { get; set; }

        public FileMappingItem() { }

        public FileMappingItem(params string[] files)
        {
            Files = new FileItems(files);
        }
    }

    public class FileItems : List<string>
    {
        private static IEnumerable<string> Empty = new List<string>();
        public FileItems(string file) : base()
        {
            this.Add(file);
        }

        public FileItems(IEnumerable<string> files) : base(files ?? Empty)
        {
        }

        public static explicit operator FileItems(string input)
        {
            return new FileItems(input);
        }
    }

    public enum FileMappingFormat
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
