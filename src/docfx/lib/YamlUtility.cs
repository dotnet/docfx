// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Newtonsoft.Json.Linq;

using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide Utilities of Yaml
    /// </summary>
    public static class YamlUtility
    {
        public const string YamlMimePrefix = "YamlMime:";

        /// <summary>
        /// Get YamlMime from TextReader
        /// </summary>
        public static string ReadMime(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            var line = reader.ReadLine();
            if (line == null || !line.StartsWith("#"))
            {
                return null;
            }
            var content = line.TrimStart('#').Trim(' ');
            if (!content.StartsWith(YamlMimePrefix))
            {
                return null;
            }
            return content;
        }

        /// <summary>
        /// Get YamlMime from yaml string
        /// </summary>
        public static string ReadMime(string yaml)
        {
            return ReadMime(new StringReader(yaml));
        }

        /// <summary>
        /// Deserialize From yaml string
        /// </summary>
        public static T Deserialize<T>(string input)
        {
            return Deserialize<T>(new StringReader(input));
        }

        /// <summary>
        /// Deserialize From TextReader
        /// </summary>
        public static T Deserialize<T>(TextReader reader)
        {
            var json = Deserialize(reader);
            return json.ToObject<T>(JsonUtililty.DefaultDeserializer);
        }

        /// <summary>
        /// Deserialize to JToken From string
        /// </summary>
        public static JToken Deserialize(string input)
        {
            return Deserialize(new StringReader(input));
        }

        /// <summary>
        /// Deserialize to JToken from TextReader
        /// </summary>
        public static JToken Deserialize(TextReader reader)
        {
            var stream = new YamlStream();

            stream.Load(reader);
            if (stream.Documents.Count != 1)
            {
                throw new NotSupportedException("Does not support mutiple YAML documents");
            }
            return ToJson(stream.Documents[0].RootNode);
        }

        private static JToken ToJson(YamlNode node)
        {
            if (node is YamlScalarNode scalar)
            {
                if (scalar.Style == ScalarStyle.Plain)
                {
                    if (string.IsNullOrWhiteSpace(scalar.Value))
                    {
                        return null;
                    }
                    if (scalar.Value == "~")
                    {
                        return null;
                    }
                    if (long.TryParse(scalar.Value, out var n))
                    {
                        return new JValue(n);
                    }
                    if (double.TryParse(scalar.Value, out var d))
                    {
                        return new JValue(d);
                    }
                    if (bool.TryParse(scalar.Value, out var b))
                    {
                        return new JValue(b);
                    }
                }
                return new JValue(scalar.Value);
            }
            if (node is YamlMappingNode map)
            {
                var obj = new JObject();
                foreach (var (key, value) in map)
                {
                    if (key is YamlScalarNode scalarKey)
                    {
                        obj[scalarKey.Value] = ToJson(value);
                    }
                    else
                    {
                        throw new NotSupportedException($"Not Supported: {key} is not a primitive type");
                    }
                }
                return obj;
            }
            if (node is YamlSequenceNode seq)
            {
                var arr = new JArray();
                foreach (var item in seq)
                {
                    arr.Add(ToJson(item));
                }
                return arr;
            }
            throw new NotSupportedException($"Unknown yaml node type {node.GetType()}");
        }
    }
}
