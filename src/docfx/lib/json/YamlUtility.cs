// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Microsoft.Docs.Build;

internal static partial class YamlUtility
{
    public const string YamlMimePrefix = "YamlMime:";

    public static string? ReadMime(TextReader reader)
    {
        var mime = ReadMime(reader.ReadLine() ?? "");
        if (string.Equals(mime, "YamlDocument", StringComparison.OrdinalIgnoreCase))
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
        return header[YamlMimePrefix.Length..].Trim();
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
        return yaml[..(i < 0 ? yaml.Length : i)].TrimStart('#').Trim();
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
        return ParseCore(errors, input, file).RemoveNulls(errors, file);
    }

    private static string? ReadDocumentType(TextReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("documentType:"))
            {
                return line["documentType:".Length..].Trim();
            }
        }
        return null;
    }

    private static JToken ParseCore(ErrorBuilder errors, TextReader input, FilePath? file)
    {
        try
        {
            var disableLineInfo = ShouldDisableLineInfo(input);

            JToken? result = null;

            var parser = new Parser(input);
            parser.Consume<StreamStart>();
            if (!parser.TryConsume<StreamEnd>(out _))
            {
                parser.Consume<DocumentStart>();
                result = ToJToken(errors, parser, file, disableLineInfo);
                parser.Consume<DocumentEnd>();
            }

            return result ?? JValue.CreateNull();
        }
        catch (YamlException ex)
        {
            var source = file is null ? null : new SourceInfo(file, ex.Start.Line, ex.Start.Column, ex.End.Line, ex.End.Column);
            var message = Regex.Replace(ex.Message, "^\\(.*?\\) - \\(.*?\\):\\s*", "");

            throw Errors.Yaml.YamlSyntaxError(source, message).ToException(ex);
        }
    }

    private static bool ShouldDisableLineInfo(TextReader reader)
    {
        // Disable accurate line info for .NET reference to reduce memory usage.
        if (reader.Peek() != '#')
        {
            return false;
        }

        var mime = ReadMime(reader.ReadLine() ?? "");
        return mime != null && mime.StartsWith("Net");
    }

    private static JToken ToJToken(ErrorBuilder errors, IParser parser, FilePath? file, bool disableLineInfo, SourceInfo? keySourceInfo = null)
    {
        switch (parser.Consume<NodeEvent>())
        {
            case Scalar scalar:
                if (scalar.Style == ScalarStyle.Plain)
                {
                    return SetSourceInfo(ParseScalar(scalar.Value), scalar);
                }
                return SetSourceInfo(new JValue(scalar.Value), scalar);

            case SequenceStart seq:
                var array = new JArray();
                while (!parser.TryConsume<SequenceEnd>(out _))
                {
                    array.Add(ToJToken(errors, parser, file, disableLineInfo));
                }
                return SetSourceInfo(array, seq);

            case MappingStart map:
                var obj = new JObject();
                while (!parser.TryConsume<MappingEnd>(out _))
                {
                    var key = parser.Consume<Scalar>();

                    if (obj.ContainsKey(key.Value))
                    {
                        errors.Add(Errors.Yaml.YamlDuplicateKey(ToSourceInfo(key, file), key.Value));
                    }

                    var value = ToJToken(errors, parser, file, disableLineInfo, ToSourceInfo(key, file));
                    obj[key.Value] = value;
                }
                return SetSourceInfo(obj, map);

            default:
                throw new NotSupportedException($"Yaml node '{parser.Current?.GetType().Name}' is not supported");
        }

        JToken SetSourceInfo(JToken token, ParsingEvent node)
        {
            return disableLineInfo ? token : JsonUtility.SetSourceInfo(token, ToSourceInfo(node, file, keySourceInfo));
        }
    }

    private static SourceInfo? ToSourceInfo(ParsingEvent node, FilePath? file, SourceInfo? keySourceInfo = null)
    {
        return file is null ? null : new SourceInfo(file, node.Start.Line, node.Start.Column, node.End.Line, node.End.Column)
        {
            KeySourceInfo = keySourceInfo,
        };
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
        if (long.TryParse(value, out var l))
        {
            return new JValue(l);
        }
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) &&
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
}
