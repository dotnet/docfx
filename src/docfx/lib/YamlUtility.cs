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
        public static (List<Error> errors, Dictionary<JToken, List<(int, int)>> mappings, T) Deserialize<T>(string input)
        {
            return Deserialize<T>(new StringReader(input));
        }

        /// <summary>
        /// Deserialize From TextReader
        /// </summary>
        public static (List<Error> errors, Dictionary<JToken, List<(int, int)>> mappings, T) Deserialize<T>(TextReader reader)
        {
            var (errors, mappings, json) = Deserialize(reader);
            return (errors, mappings, json.ToObject<T>(JsonUtility.DefaultDeserializer));
        }

        /// <summary>
        /// Deserialize to JToken From string
        /// </summary>
        public static (List<Error> errors, Dictionary<JToken, List<(int, int)>> mappings, JToken) Deserialize(string input)
        {
            return Deserialize(new StringReader(input));
        }

        /// <summary>
        /// Deserialize to JToken from TextReader
        /// </summary>
        public static (List<Error> errors, Dictionary<JToken, List<(int, int)>> mappings, JToken token) Deserialize(TextReader reader)
        {
            var mappings = new Dictionary<JToken, List<(int, int)>>();
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
                return (errors, mappings, JValue.CreateNull());
            }

            if (stream.Documents.Count != 1)
            {
                throw new NotSupportedException("Does not support mutiple YAML documents");
            }
            return (errors, mappings, ToJson(stream.Documents[0].RootNode, mappings));
        }

        private static JToken ToJson(YamlNode node, Dictionary<JToken, List<(int, int)>> mappings)
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

        private static void SetMappings(Dictionary<JToken, List<(int, int)>> mappings, YamlNode scalar, JToken value)
        {
            if (mappings.ContainsKey(value))
            {
                mappings[value].Add((scalar.Start.Line, scalar.Start.Column));
            }
            else
            {
                mappings.Add(value, new List<(int, int)> { (scalar.Start.Line, scalar.Start.Column) });
            }
        }
    }
}
