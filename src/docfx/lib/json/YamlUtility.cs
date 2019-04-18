// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
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

        public static string ReadMime(TextReader reader)
        {
            return ReadMime(reader.ReadLine());
        }

        /// <summary>
        /// Get yaml mime type
        /// </summary>
        public static string ReadMime(string yaml)
        {
            var header = ReadHeader(yaml);
            if (header is null || !header.StartsWith(YamlMimePrefix, StringComparison.OrdinalIgnoreCase))
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
        /// De-serialize from yaml string, which is not user input
        /// schema validation errors will be ignored, syntax errors and type mismatching will be thrown
        /// </summary>
        public static T Deserialize<T>(string input)
        {
            var token = ParseAsJToken(input, file: null);
            return token.ToObject<T>(JsonUtility.Serializer);
        }

        /// <summary>
        /// Deserialize from a YAML file, get from or add to cache
        /// </summary>
        public static (List<Error>, JToken) Parse(Document file, Context context) => context.Cache.LoadYamlFile(file);

        /// <summary>
        /// Deserialize to JToken from string
        /// </summary>
        public static (List<Error>, JToken) Parse(string input, string file)
        {
            return ParseAsJToken(input, file).RemoveNulls();
        }

        private static JToken ParseAsJToken(string input, string file)
        {
            Match match;

            var stream = new YamlStream();

            try
            {
                stream.Load(new StringReader(input));
            }
            catch (YamlException ex) when (
                ex.InnerException is ArgumentException aex &&
                (match = Regex.Match(aex.Message, "(.*?)\\. Key: (.*)$")).Success)
            {
                var source = new SourceInfo(file, ex.Start.Line, ex.Start.Column, ex.End.Line, ex.End.Column);

                throw Errors.YamlDuplicateKey(source, match.Groups[2].Value).ToException(ex);
            }
            catch (YamlException ex)
            {
                var source = new SourceInfo(file, ex.Start.Line, ex.Start.Column, ex.End.Line, ex.End.Column);
                var message = Regex.Replace(ex.Message, "^\\(.*?\\) - \\(.*?\\):\\s*", "");

                throw Errors.YamlSyntaxError(source, message).ToException(ex);
            }

            if (stream.Documents.Count == 0)
            {
                return JValue.CreateNull();
            }

            if (stream.Documents.Count != 1)
            {
                throw new NotSupportedException("Does not support mutiple YAML documents");
            }

            return ToJson(stream.Documents[0].RootNode, file);
        }

        private static JToken ToJson(YamlNode node, string file)
        {
            if (node is YamlScalarNode scalar)
            {
                if (scalar.Style == ScalarStyle.Plain)
                {
                    if (string.IsNullOrWhiteSpace(scalar.Value) ||
                        scalar.Value == "~" ||
                        string.Equals(scalar.Value, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        return SetSourceInfo(JValue.CreateNull(), node, file);
                    }
                    if (long.TryParse(scalar.Value, out var n))
                    {
                        return SetSourceInfo(new JValue(n), node, file);
                    }
                    if (double.TryParse(scalar.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    {
                        return SetSourceInfo(new JValue(d), node, file);
                    }
                    if (bool.TryParse(scalar.Value, out var b))
                    {
                        return SetSourceInfo(new JValue(b), node, file);
                    }
                }
                return SetSourceInfo(new JValue(scalar.Value), node, file);
            }
            if (node is YamlMappingNode map)
            {
                var obj = new JObject();
                foreach (var (key, value) in map)
                {
                    if (key is YamlScalarNode scalarKey)
                    {
                        var token = ToJson(value, file);
                        var prop = SetSourceInfo(new JProperty(scalarKey.Value, token), key, file);
                        obj.Add(prop);
                    }
                    else
                    {
                        throw new NotSupportedException($"Not Supported: {key} is not a primitive type");
                    }
                }

                return SetSourceInfo(obj, node, file);
            }
            if (node is YamlSequenceNode seq)
            {
                var arr = new JArray();
                foreach (var item in seq)
                {
                    arr.Add(ToJson(item, file));
                }
                return SetSourceInfo(arr, node, file);
            }
            throw new NotSupportedException($"Unknown yaml node type {node.GetType()}");
        }

        private static JToken SetSourceInfo(JToken token, YamlNode node, string file)
        {
            return JsonUtility.SetSourceInfo(
                token,
                new SourceInfo(file, node.Start.Line, node.Start.Column, node.End.Line, node.End.Column));
        }
    }
}
