// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
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
        private static readonly MethodInfo s_setLineInfo = typeof(JToken).GetMethod(
            "SetLineInfo",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(int), typeof(int) },
            null);

        /// <summary>
        /// Get yaml mime type
        /// </summary>
        public static string ReadMime(string yaml)
        {
            var header = ReadHeader(yaml);
            if (header == null || !header.StartsWith(YamlMimePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return header.Substring(YamlMimePrefix.Length).Trim();
        }

        /// <summary>
        /// Get the content of the first comment line
        /// </summary>
        public static string ReadHeader(string yaml)
        {
            if (!yaml.StartsWith("#"))
            {
                return null;
            }
            var i = yaml.IndexOf('\n');
            return yaml.Substring(0, i < 0 ? yaml.Length : i).TrimStart('#').Trim();
        }

        /// <summary>
        /// Deserialize From yaml string
        /// </summary>
        public static (List<Error>, T) Deserialize<T>(string input, bool nullValidation = true)
        {
            var (errors, json) = Deserialize(input, nullValidation);
            try
            {
                var result = json.ToObject<T>(JsonUtility.MissingMemberErrorDeserializer);
                return (errors, result);
            }
            catch (JsonSerializationException ex)
            {
                errors.Add(Errors.InValidSchema(ParseRangeFromExceptionMessage(ex.Message), ex.Message));
                var result = json.ToObject<T>(JsonUtility.DefaultDeserializer);
                return (errors, result);
            }
        }

        /// <summary>
        /// Deserialize to JToken from string
        /// </summary>
        public static (List<Error>, JToken) Deserialize(string input, bool nullValidation = true)
        {
            var errors = new List<Error>();
            var stream = new YamlStream();

            try
            {
                stream.Load(new StringReader(input));
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
                var (nullErrors, token) = ToJson(stream.Documents[0].RootNode).ValidateNullValue();
                errors.AddRange(nullErrors);
                return (errors, token);
            }
            else
            {
                var token = ToJson(stream.Documents[0].RootNode);
                return (errors, token);
            }
        }

        private static Range ParseRangeFromExceptionMessage(string message)
        {
            var parts = message.Remove(message.Length - 1).Split(',');
            var lineNumber = int.Parse(parts.SkipLast(1).Last().Split(' ').Last());
            var linePosition = int.Parse(parts.Last().Split(' ').Last());
            return new Range(lineNumber, linePosition);
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
                        return PopulateLineInfoToJToken(new JValue(n), node);
                    }
                    if (double.TryParse(scalar.Value, out var d))
                    {
                        return PopulateLineInfoToJToken(new JValue(d), node);
                    }
                    if (bool.TryParse(scalar.Value, out var b))
                    {
                        return PopulateLineInfoToJToken(new JValue(b), node);
                    }
                }
                return PopulateLineInfoToJToken(new JValue(scalar.Value), node);
            }
            if (node is YamlMappingNode map)
            {
                var obj = new JObject();
                foreach (var (key, value) in map)
                {
                    if (key is YamlScalarNode scalarKey)
                    {
                        var token = ToJson(value);
                        var prop = PopulateLineInfoToJToken(new JProperty(scalarKey.Value, token), key);
                        obj.Add(prop);
                    }
                    else
                    {
                        throw new NotSupportedException($"Not Supported: {key} is not a primitive type");
                    }
                }

                return PopulateLineInfoToJToken(obj, node);
            }
            if (node is YamlSequenceNode seq)
            {
                var arr = new JArray();
                foreach (var item in seq)
                {
                    arr.Add(ToJson(item));
                }
                return PopulateLineInfoToJToken(arr, node);
            }
            throw new NotSupportedException($"Unknown yaml node type {node.GetType()}");
        }

        private static JToken PopulateLineInfoToJToken(JToken token, YamlNode node)
        {
            if (token is null)
                return token;

            s_setLineInfo.Invoke(token, new object[] { node.Start.Line, node.Start.Column });
            return token;
        }
    }
}
