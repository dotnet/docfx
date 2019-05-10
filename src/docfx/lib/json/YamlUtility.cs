// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Microsoft.Docs.Build
{
    internal static class YamlUtility
    {
        public const string YamlMimePrefix = "YamlMime:";

        public static string ReadMime(TextReader reader)
        {
            var mime = ReadMime(reader.ReadLine());
            if (string.Compare(mime, "YamlDocument", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return ReadDocumentType(reader);
            }
            return mime;
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
        public static T Deserialize<T>(string input, string file)
        {
            var (_, token) = ParseAsJToken(input, file);
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
            var (errors, token) = ParseAsJToken(input, file);
            var (nullErrors, result) = token.RemoveNulls();
            errors.AddRange(nullErrors);
            return (errors, result);
        }

        private static string ReadDocumentType(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("documentType:"))
                {
                    return line.Substring("documentType:".Length).Trim();
                }
            }
            return null;
        }

        private static (List<Error>, JToken) ParseAsJToken(string input, string file)
        {
            try
            {
                JToken result = null;

                var errors = new List<Error>();
                var parser = new Parser(new StringReader(input));
                parser.Expect<StreamStart>();
                if (!parser.Accept<StreamEnd>())
                {
                    parser.Expect<DocumentStart>();
                    result = ParseAsJToken(parser, file, errors);
                    parser.Expect<DocumentEnd>();
                }
                parser.Expect<StreamEnd>();

                return (errors, result);
            }
            catch (YamlException ex)
            {
                var source = new SourceInfo(file, ex.Start.Line, ex.Start.Column, ex.End.Line, ex.End.Column);
                var message = Regex.Replace(ex.Message, "^\\(.*?\\) - \\(.*?\\):\\s*", "");

                throw Errors.YamlSyntaxError(source, message).ToException(ex);
            }
        }

        private static JToken ParseAsJToken(IParser parser, string file, List<Error> errors)
        {
            switch (parser.Expect<NodeEvent>())
            {
                case Scalar scalar:
                    if (scalar.Style == ScalarStyle.Plain)
                    {
                        return SetSourceInfo(ParseScalar(scalar.Value), scalar, file);
                    }
                    return SetSourceInfo(new JValue(scalar.Value), scalar, file);

                case SequenceStart seq:
                    var array = new JArray();
                    while (!parser.Accept<SequenceEnd>())
                    {
                        array.Add(ParseAsJToken(parser, file, errors));
                    }
                    parser.Expect<SequenceEnd>();
                    return SetSourceInfo(array, seq, file);

                case MappingStart map:
                    var obj = new JObject();
                    while (!parser.Accept<MappingEnd>())
                    {
                        var key = parser.Expect<Scalar>();
                        var value = ParseAsJToken(parser, file, errors);

                        if (obj.ContainsKey(key.Value))
                        {
                            var source = new SourceInfo(file, key.Start.Line, key.Start.Column, key.End.Line, key.End.Column);
                            errors.Add(Errors.YamlDuplicateKey(source, key.Value));
                        }

                        obj[key.Value] = value;
                        SetSourceInfo(obj.Property(key.Value), key, file);
                    }
                    parser.Expect<MappingEnd>();
                    return SetSourceInfo(obj, map, file);

                default:
                    throw new NotSupportedException($"Yaml node '{parser.Current.GetType().Name}' is not supported");
            }
        }

        private static JToken ParseScalar(string value)
        {
            // https://yaml.org/spec/1.2/2009-07-21/spec.html
            //
            //  Regular expression       Resolved to tag
            //
            //    null | Null | NULL | ~                          tag:yaml.org,2002:null
            //    /* Empty */                                     tag:yaml.org,2002:null
            //    true | True | TRUE | false | False | FALSE      tag:yaml.org,2002:bool
            //    [-+]?[0 - 9]+                                   tag:yaml.org,2002:int(Base 10)
            //    0o[0 - 7] +                                     tag:yaml.org,2002:int(Base 8)
            //    0x[0 - 9a - fA - F] +                           tag:yaml.org,2002:int(Base 16)
            //    [-+] ? ( \. [0-9]+ | [0-9]+ ( \. [0-9]* )? ) ( [eE][-+]?[0 - 9]+ )?   tag:yaml.org,2002:float (Number)
            //    [-+]? ( \.inf | \.Inf | \.INF )                 tag:yaml.org,2002:float (Infinity)
            //    \.nan | \.NaN | \.NAN                           tag:yaml.org,2002:float (Not a number)
            //    *                                               tag:yaml.org,2002:str(Default)
            if (string.IsNullOrEmpty(value) || value == "~" || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return JValue.CreateNull();
            }
            if (bool.TryParse(value, out var b))
            {
                return new JValue(b);
            }
            if (int.TryParse(value, out var n))
            {
                return new JValue(n);
            }
            if (long.TryParse(value, out var l))
            {
                return new JValue(l);
            }
            if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) &&
                !double.IsNaN(d) && !double.IsPositiveInfinity(d) && !double.IsNegativeInfinity(d))
            {
                return new JValue(d);
            }
            if (value.Equals(".nan", StringComparison.OrdinalIgnoreCase))
            {
                return new JValue(double.NaN);
            }
            if (value.Equals(".inf", StringComparison.OrdinalIgnoreCase) || value.Equals("+.inf", StringComparison.OrdinalIgnoreCase))
            {
                return new JValue(double.PositiveInfinity);
            }
            if (value.Equals("-.inf", StringComparison.OrdinalIgnoreCase))
            {
                return new JValue(double.NegativeInfinity);
            }
            return new JValue(value);
        }

        private static JToken SetSourceInfo(JToken token, ParsingEvent node, string file)
        {
            return JsonUtility.SetSourceInfo(
                token,
                new SourceInfo(file, node.Start.Line, node.Start.Column, node.End.Line, node.End.Column));
        }
    }
}
