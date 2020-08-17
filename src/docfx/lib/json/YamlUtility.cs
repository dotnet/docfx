// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;

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
        public static T DeserializeData<T>(string data, FilePath? file) where T : class, new()
        {
            using var reader = new StringReader(data);
            return DeserializeData<T>(reader, file);
        }

        /// <summary>
        /// De-serialize from yaml string, which is not user input
        /// schema validation errors will be ignored, syntax errors and type mismatching will be thrown
        /// </summary>
        public static T DeserializeData<T>(TextReader data, FilePath? file) where T : class, new()
        {
            return ParseCore(ErrorBuilder.Null, data, file)?.ToObject<T>(JsonUtility.Serializer) ?? new T();
        }

        public static T Deserialize<T>(ErrorBuilder errors, string input, FilePath file) where T : class, new()
        {
            using var reader = new StringReader(input);
            return Deserialize<T>(errors, reader, file);
        }

        public static T Deserialize<T>(ErrorBuilder errors, TextReader reader, FilePath file) where T : class, new()
        {
            return JsonUtility.ToObject<T>(errors, Parse(errors, reader, file));
        }

        /// <summary>
        /// Deserialize to JToken from string
        /// </summary>
        public static JToken Parse(ErrorBuilder errors, string input, FilePath? file)
        {
            return Parse(errors, new StringReader(input), file);
        }

        /// <summary>
        /// Deserialize to JToken from string
        /// </summary>
        public static JToken Parse(ErrorBuilder errors, TextReader input, FilePath? file)
        {
            return ParseCore(errors, input, file).RemoveNulls(errors);
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

        private static JToken ParseCore(ErrorBuilder errors, TextReader input, FilePath? file)
        {
            try
            {
                var result = ToJToken(
                    input,
                    onKeyDuplicate: key => errors.Add(Errors.Yaml.YamlDuplicateKey(ToSourceInfo(key, file), key.Value)),
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

                return result;
            }
            catch (YamlException ex)
            {
                var source = file is null ? null : new SourceInfo(file, ex.Start.Line, ex.Start.Column, ex.End.Line, ex.End.Column);
                var message = Regex.Replace(ex.Message, "^\\(.*?\\) - \\(.*?\\):\\s*", "");

                throw Errors.Yaml.YamlSyntaxError(source, message).ToException(ex);
            }
        }

        private static SourceInfo? ToSourceInfo(ParsingEvent node, FilePath? file)
        {
            return file is null ? null : new SourceInfo(file, node.Start.Line, node.Start.Column, node.End.Line, node.End.Column);
        }
    }
}
