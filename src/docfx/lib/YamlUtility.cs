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
                errors.Add(Errors.YamlDuplicateKey(ParseDuplicateKeyFromErrorMessage(ex.Message + ex.InnerException?.Message)));
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

            if (nullValidation)
            {
                var mappings = new JTokenSourceMap();
                var (nullErrors, token) = ToJson(stream.Documents[0].RootNode, mappings).ValidateNullValue(mappings);
                errors.AddRange(nullErrors);
                return (errors, token);
            }
            else
            {
                return (errors, ToJson(stream.Documents[0].RootNode));
            }
        }

        private static string ParseDuplicateKeyFromErrorMessage(string message)
        {
            var index = message.LastIndexOf(':');
            return message.Substring(index + 1, message.Length - index - 1);
        }

        private static JToken ToJson(YamlNode node, JTokenSourceMap mappings = null)
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
                        return SetMappings(mappings, scalar, new JValue(n));
                    }
                    if (double.TryParse(scalar.Value, out var d))
                    {
                        return SetMappings(mappings, scalar, new JValue(d));
                    }
                    if (bool.TryParse(scalar.Value, out var b))
                    {
                        return SetMappings(mappings, scalar, new JValue(b));
                    }
                }
                return SetMappings(mappings, scalar, new JValue(scalar.Value));
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
                return SetMappings(mappings, node, obj);
            }
            if (node is YamlSequenceNode seq)
            {
                var arr = new JArray();
                foreach (var item in seq)
                {
                    arr.Add(ToJson(item, mappings));
                }
                return SetMappings(mappings, node, arr);
            }
            throw new NotSupportedException($"Unknown yaml node type {node.GetType()}");
        }

        private static JToken SetMappings(JTokenSourceMap mappings, YamlNode scalar, JToken value)
        {
            if (mappings == null)
                return value;

            mappings.Add(value, new Range(scalar.Start.Line, scalar.Start.Column));
            return value;
        }
    }
}
