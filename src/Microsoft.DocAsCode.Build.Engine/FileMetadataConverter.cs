// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Glob;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class FileMetadataConverter : JsonConverter
    {
        private const string BaseDir = "baseDir";
        private const string Dict = "dict";
        private const string Glob = "glob";
        private const string Key = "key";
        private const string Value = "value";

        private readonly bool _ignoreBaseDir;

        public FileMetadataConverter() : base() { }

        public FileMetadataConverter(bool ignoreBaseDir)
        {
            _ignoreBaseDir = ignoreBaseDir;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FileMetadata);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token;
            if (reader.TokenType == JsonToken.StartObject)
            {
                token = JToken.Load(reader);
            }
            else
            {
                throw new JsonReaderException($"{reader.TokenType.ToString()} is not a valid {objectType.Name}.");
            }
            var baseDir = (string)((JObject)token).GetValue(BaseDir);
            if (!(token[Dict] is JObject dict))
            {
                throw new JsonReaderException($"Expect {token[Dict]} to be JObject.");
            }
            var metaDict = new Dictionary<string, ImmutableArray<FileMetadataItem>>();
            foreach (var pair in dict)
            {
                metaDict.Add(pair.Key, GetFileMetadataItemArray(pair.Value));
            }

            return new FileMetadata(baseDir, metaDict);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var fileMetadata = (FileMetadata)value;
            writer.WriteStartObject();

            if (!_ignoreBaseDir && fileMetadata.BaseDir != null)
            {
                writer.WritePropertyName(BaseDir);
                writer.WriteRawValue(JsonUtility.Serialize(fileMetadata.BaseDir));
            }

            writer.WritePropertyName(Dict);
            writer.WriteStartObject();
            foreach (var pair in fileMetadata)
            {
                writer.WritePropertyName(pair.Key);
                writer.WriteStartArray();
                foreach (var item in pair.Value)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(Glob);
                    writer.WriteRawValue(JsonUtility.Serialize(item.Glob.Raw));
                    writer.WritePropertyName(Key);
                    writer.WriteRawValue(JsonUtility.Serialize(item.Key));
                    writer.WritePropertyName(Value);
                    writer.WriteRawValue(JsonUtility.Serialize(item.Value));
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        private ImmutableArray<FileMetadataItem> GetFileMetadataItemArray(JToken value)
        {
            if (!(value is JArray arr))
            {
                throw new JsonReaderException($"Expect {value} to be JArray.");
            }
            return arr.Select(e =>
            {
                if (!(e is JObject obj))
                {
                    throw new JsonReaderException($"Expect {e} to be JObject.");
                }
                return new FileMetadataItem(
                    new GlobMatcher((string)obj[Glob]),
                    (string)obj[Key],
                    ConvertToObjectHelper.ConvertJObjectToObject(obj[Value]));
            }).ToImmutableArray();
        }
    }
}
