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
        public static (List<Error> errors, T) Deserialize<T>(string input)
        {
            return Deserialize<T>(new StringReader(input));
        }

        /// <summary>
        /// Deserialize From TextReader
        /// </summary>
        public static (List<Error> errors, T) Deserialize<T>(TextReader reader)
        {
            var (errors, json) = Deserialize(reader);
            return (errors, json.ToObject<T>(JsonUtility.DefaultDeserializer));
        }

        /// <summary>
        /// Deserialize to JToken From string
        /// </summary>
        public static (List<Error> errors, JToken) Deserialize(string input)
        {
            return Deserialize(new StringReader(input));
        }

        /// <summary>
        /// Deserialize to JToken from TextReader
        /// </summary>
        public static (List<Error> errors, JToken token) Deserialize(TextReader reader)
        {
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
                return (errors, JValue.CreateNull());
            }

            if (stream.Documents.Count != 1)
            {
                throw new NotSupportedException("Does not support mutiple YAML documents");
            }

            var (jErrors, json) = ToJson(stream.Documents[0].RootNode);
            errors.AddRange(jErrors);
            return (errors, json);
        }

        private static (List<Error>, JToken) ToJson(YamlNode node)
        {
            var errors = new List<Error>();
            if (node is YamlScalarNode scalar)
            {
                return (errors, ParseYamlScalarNode(scalar));
            }
            if (node is YamlMappingNode map)
            {
                return (errors, ParseYamlMappingNode(errors, map));
            }
            if (node is YamlSequenceNode seq)
            {
                return (errors, ParseYamlSequenceNode(errors, seq));
            }
            throw new NotSupportedException($"Unknown yaml node type {node.GetType()}");
        }

        private static JToken ParseYamlSequenceNode(List<Error> errors, YamlSequenceNode seq)
        {
            var arr = new JArray();
            foreach (var item in seq)
            {
                if (string.IsNullOrEmpty(item.ToString()) || item.ToString() == "null")
                {
                    errors.Add(Errors.NullValue(item.Start, item.End));
                }
                else
                {
                    var (e, json) = ToJson(item);
                    errors.AddRange(e);
                    arr.Add(json);
                }
            }
            return arr;
        }

        private static JToken ParseYamlMappingNode(List<Error> errors, YamlMappingNode map)
        {
            var obj = new JObject();
            foreach (var (key, value) in map)
            {
                if (key is YamlScalarNode scalarKey)
                {
                    if (string.IsNullOrEmpty(value.ToString()) || value.ToString() == "null")
                    {
                        errors.Add(Errors.NullValue(value.Start, value.End));
                    }
                    else
                    {
                        List<Error> e;
                        (e, obj[scalarKey.Value]) = ToJson(value);
                        errors.AddRange(e);
                    }
                }
                else
                {
                    throw new NotSupportedException($"Not Supported: {key} is not a primitive type");
                }
            }
            return obj;
        }

        private static JToken ParseYamlScalarNode(YamlScalarNode scalar)
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
    }
}
