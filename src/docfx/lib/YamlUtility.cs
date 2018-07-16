// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        public static (List<Error>, T) Deserialize<T>(string input, bool nullValidation = true)
        {
            return Deserialize<T>(new StringReader(input), nullValidation);
        }

        /// <summary>
        /// Deserialize From TextReader
        /// </summary>
        public static (List<Error>, T) Deserialize<T>(TextReader reader, bool nullValidation = true)
        {
            var (errors, json) = Deserialize(reader, nullValidation);
            return (errors, json.ToObject<T>(JsonUtility.DefaultDeserializer));
        }

        /// <summary>
        /// Deserialize to JToken From string
        /// </summary>
        public static (List<Error>, JToken) Deserialize(string input, bool nullValidation = true)
        {
            return Deserialize(new StringReader(input), nullValidation);
        }

        /// <summary>
        /// Deserialize to JToken from TextReader
        /// </summary>
        public static (List<Error>, JToken) Deserialize(TextReader reader, bool nullValidation = true)
        {
            var errors = new List<Error>();
            var stream = new YamlStream();

            try
            {
                stream.Load(reader);
            }
            catch (YamlException ex) when (ex.Message.Contains("Duplicate key"))
            {
                throw Errors.YamlDuplicateKey(ex).ToException();
            }
            catch (YamlException ex)
            {
                throw Errors.YamlSyntaxError(ex).ToException();
            }

            if (stream.Documents.Count == 0)
            {
                return (errors, JValue.CreateNull());
            }

            if (stream.Documents.Count != 1)
            {
                throw new NotSupportedException("Does not support mutiple YAML documents");
            }

            if (nullValidation)
            {
                var mappings = new Dictionary<string, Range>();
                var (nullErrors, token) = ToJson(stream.Documents[0].RootNode, mappings: mappings).ValidateNullValue(mappings);
                errors.AddRange(nullErrors);
                return (errors, token);
            }
            else
            {
                return (errors, ToJson(stream.Documents[0].RootNode));
            }
        }

        private static JToken ToJson(YamlNode node, string name = null, IDictionary<string, Range> mappings = null)
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
                        var path = string.Join('|', name, key.ToString());
                        SetMappings(mappings, key, path);
                        var jToken = ToJson(value, path, mappings: mappings);
                        obj[scalarKey.Value] = jToken;
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

                // Adding index to path to have unique path for array items
                var index = 0;
                foreach (var item in seq)
                {
                    arr.Add(ToJson(item, string.Join('|', name, index++), mappings: mappings));
                }
                return arr;
            }
            throw new NotSupportedException($"Unknown yaml node type {node.GetType()}");
        }

        private static void SetMappings(IDictionary<string, Range> mappings, YamlNode scalar, string name)
        {
            if (mappings == null)
                return;

            mappings.Add(name, new Range(scalar.Start.Line, scalar.Start.Column));
        }
    }
}
