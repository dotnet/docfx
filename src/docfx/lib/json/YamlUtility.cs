// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static partial class YamlUtility
    {
        public const string YamlMimePrefix = "YamlMime:";

        public static string? ReadMime(TextReader reader)
        {
            var mime = ReadMime(reader.ReadLine() ?? "");
            if (string.Compare(mime, "YamlDocument", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return ReadDocumentType(reader);
            }
            return mime;
        }

        /// <summary>
        /// Get yaml mime type
        /// </summary>
        public static string? ReadMime(string yaml)
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
        public static string? ReadHeader(string yaml)
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
        public static T Deserialize<T>(string input, FilePath? file) where T : class, new()
        {
            using var reader = new StringReader(input);
            return Deserialize<T>(reader, file);
        }

        /// <summary>
        /// De-serialize from yaml string, which is not user input
        /// schema validation errors will be ignored, syntax errors and type mismatching will be thrown
        /// </summary>
        public static T Deserialize<T>(TextReader input, FilePath? file) where T : class, new()
        {
            var (_, token) = ParseAsJToken(input, file);
            return token?.ToObject<T>(JsonUtility.Serializer) ?? new T();
        }

        /// <summary>
        /// Deserialize to JToken from string
        /// </summary>
        public static (List<Error>, JToken) Parse(string input, FilePath? file)
        {
            return Parse(new StringReader(input), file);
        }

        /// <summary>
        /// Deserialize to JToken from string
        /// </summary>
        public static (List<Error>, JToken) Parse(TextReader input, FilePath? file)
        {
            var (errors, token) = ParseAsJToken(input, file);
            var (nullErrors, result) = token.RemoveNulls();
            errors.AddRange(nullErrors);
            return (errors, result);
        }

        private static string? ReadDocumentType(TextReader reader)
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("documentType:"))
                {
                    return line.Substring("documentType:".Length).Trim();
                }
            }
            return null;
        }

        private static (List<Error>, JToken) ParseAsJToken(TextReader input, FilePath? file)
        {
            try
            {
                var errors = new List<Error>();
                var result = ToJToken(
                    input,
                    onKeyDuplicate: key => errors.Add(Errors.YamlDuplicateKey(ToSourceInfo(key, file), key.Value)),
                    onConvert: (token, node) =>
                    {
                        if (token is JProperty property)
                        {
                            var sourceInfo = JsonUtility.GetSourceInfo(property.Value);
                            if (sourceInfo != null)
                            {
                                sourceInfo.KeySourceInfo = ToSourceInfo(node, file);
                            }
                            return token;
                        }
                        return JsonUtility.SetSourceInfo(token, ToSourceInfo(node, file));
                    });

                return (errors, result);
            }
            catch (YamlException ex)
            {
                var source = file is null ? null : new SourceInfo(file, ex.Start.Line, ex.Start.Column, ex.End.Line, ex.End.Column);
                var message = Regex.Replace(ex.Message, "^\\(.*?\\) - \\(.*?\\):\\s*", "");

                throw Errors.YamlSyntaxError(source, message).ToException(ex);
            }
        }

        private static SourceInfo? ToSourceInfo(ParsingEvent node, FilePath? file)
        {
            return file is null ? null : new SourceInfo(file, node.Start.Line, node.Start.Column, node.End.Line, node.End.Column);
        }
    }
}
