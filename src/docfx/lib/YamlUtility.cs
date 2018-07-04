// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json.Linq;

using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide Utilities of Yaml
    /// </summary>
    internal static class YamlUtility
    {
        public const string YamlMimePrefix = "YamlMime:";

        /// <summary>
        /// Get YamlMime from TextReader
        /// </summary>
        public static string ReadMime(TextReader reader)
        {
            var content = ReadHeader(reader);
            if (!content.StartsWith(YamlMimePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return content;
        }

        /// <summary>
        /// Get the content of the first comment line
        /// </summary>
        public static string ReadHeader(TextReader reader)
        {
            var line = reader.ReadLine();
            if (line == null || !line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return line.TrimStart('#').Trim();
        }

        /// <summary>
        /// Get YamlMime from yaml string
        /// </summary>
        public static string ReadMime(string yaml)
        {
            return ReadMime(new StringReader(yaml));
        }

        /// <summary>
        /// Get the content of the first comment line
        /// </summary>
        public static string ReadHeader(string yaml)
        {
            return ReadHeader(new StringReader(yaml));
        }

        /// <summary>
        /// Deserialize From yaml string
        /// </summary>
        public static (List<Error> errors, T, Dictionary<MappingKey, LineInfo> mappings) Deserialize<T>(string input)
        {
            return Deserialize<T>(new StringReader(input));
        }

        /// <summary>
        /// Deserialize From TextReader
        /// </summary>
        public static (List<Error> errors, T, Dictionary<MappingKey, LineInfo> mappings) Deserialize<T>(TextReader reader)
        {
            var (errors, json, mappings) = Deserialize(reader);
            return (errors, json.ToObject<T>(JsonUtility.DefaultDeserializer), mappings);
        }

        /// <summary>
        /// Deserialize to JToken From string
        /// </summary>
        public static (List<Error> errors, JToken jtoken, Dictionary<MappingKey, LineInfo> mappings) Deserialize(string input)
        {
            return Deserialize(new StringReader(input));
        }

        /// <summary>
        /// Deserialize to JToken from TextReader
        /// </summary>
        public static (List<Error> errors, JToken token, Dictionary<MappingKey, LineInfo> mappings) Deserialize(TextReader reader)
        {
            var mappings = new Dictionary<MappingKey, LineInfo>();
            var errors = new List<Error>();
            var stream = new YamlStream();

            try
            {
                stream.Load(reader);
            }
            catch (YamlException ex)
            {
                errors.Add(Errors.YamlSyntaxError(ex));
            }

            if (stream.Documents.Count == 0)
            {
                return (errors, JValue.CreateNull(), mappings);
            }

            if (stream.Documents.Count != 1)
            {
                throw new NotSupportedException("Does not support mutiple YAML documents");
            }
            return (errors, ToJson(stream.Documents[0].RootNode, mappings), mappings);
        }

        private static JToken ToJson(YamlNode node, Dictionary<MappingKey, LineInfo> mappings)
        {
            if (node is YamlScalarNode scalar)
            {
                JValue value;
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
                        value = new JValue(n);
                        SetMappings(mappings, scalar, value);
                        return value;
                    }
                    if (double.TryParse(scalar.Value, out var d))
                    {
                        value = new JValue(d);
                        SetMappings(mappings, scalar, value);
                        return value;
                    }
                    if (bool.TryParse(scalar.Value, out var b))
                    {
                        value = new JValue(b);
                        SetMappings(mappings, scalar, value);
                        return value;
                    }
                }
                value = new JValue(scalar.Value);
                SetMappings(mappings, scalar, value);
                return value;
            }
            if (node is YamlMappingNode map)
            {
                var obj = new JObject();
                foreach (var (key, value) in map)
                {
                    if (key is YamlScalarNode scalarKey)
                    {
                        var jToken = ToJson(value, mappings);
                        obj[scalarKey.Value] = jToken;
                    }
                    else
                    {
                        throw new NotSupportedException($"Not Supported: {key} is not a primitive type");
                    }
                }
                SetMappings(mappings, node, obj);
                return obj;
            }
            if (node is YamlSequenceNode seq)
            {
                var arr = new JArray();
                foreach (var item in seq)
                {
                    arr.Add(ToJson(item, mappings));
                }
                SetMappings(mappings, node, arr);
                return arr;
            }
            throw new NotSupportedException($"Unknown yaml node type {node.GetType()}");
        }

        private static void SetMappings(Dictionary<MappingKey, LineInfo> mappings, YamlNode scalar, JToken value)
        {
            mappings.Add(new MappingKey { Key = value }, new LineInfo(scalar.Start.Line, scalar.Start.Column));
        }
    }
}
