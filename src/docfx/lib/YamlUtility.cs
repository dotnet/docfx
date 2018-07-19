// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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
                var root = stream.Documents[0].RootNode;
                var (nullErrors, token) = PopulateLineInfoToJToken(ToJson(root), root).ValidateNullValue();
                errors.AddRange(nullErrors);
                return (errors, token);
            }
            else
            {
                var token = ToJson(stream.Documents[0].RootNode);
                return (errors, token);
            }
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
                        var token = ToJson(value);
                        if (token != null)
                        {
                            token = PopulateLineInfoToJToken(token, value);
                        }
                        obj[scalarKey.Value] = token;
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
                return arr;
            }
            throw new NotSupportedException($"Unknown yaml node type {node.GetType()}");
        }

        private static JToken PopulateLineInfoToJToken(JToken token, YamlNode node)
        {
            var reader = new JTokenLineInfoReader(token.CreateReader(), token, node.Start.Line, node.Start.Column);
            var result = JToken.Load(reader, new JsonLoadSettings { LineInfoHandling = LineInfoHandling.Load });
            return result;
        }

        private class JTokenLineInfoReader : JsonReader, IJsonLineInfo
        {
            private readonly JsonReader _reader;
            private readonly JToken _root;
            private readonly int _lineNumber;
            private readonly int _linePosition;
            private JToken _parent;
            private JToken _current;

            public JTokenLineInfoReader(JsonReader reader, JToken token, int lineNumber, int linePosition)
            {
                Debug.Assert(token != null);

                _root = token;
                _reader = reader;
                _lineNumber = lineNumber;
                _linePosition = linePosition;
            }

            public int LineNumber
            {
                get
                {
                    if (_current == _root)
                    {
                        return _lineNumber;
                    }
                    else
                    {
                        return (_current as IJsonLineInfo).LineNumber;
                    }
                }
            }

            public int LinePosition
            {
                get
                {
                    if (_current == _root)
                    {
                        return _linePosition;
                    }
                    else
                    {
                        return (_current as IJsonLineInfo).LinePosition;
                    }
                }
            }

            public bool HasLineInfo() => true;

            public override string Path => _reader.Path;

            public override bool Read()
            {
                if (CurrentState != State.Start)
                {
                    if (_current == null)
                    {
                        return false;
                    }

                    if (_current is JContainer container && _parent != container)
                    {
                        return ReadInto(container);
                    }
                    else
                    {
                        return ReadOver(_current);
                    }
                }

                _current = _root;
                SetToken(_current);
                return true;
            }

            private bool ReadOver(JToken t)
            {
                if (t == _root)
                {
                    return ReadToEnd();
                }

                JToken next = t.Next;
                if ((next == null || next == t) || t == t.Parent.Last)
                {
                    if (t.Parent == null)
                    {
                        return ReadToEnd();
                    }

                    return SetEnd(t.Parent);
                }
                else
                {
                    _current = next;
                    SetToken(_current);
                    return true;
                }
            }

            private bool ReadToEnd()
            {
                _current = null;
                SetToken(JsonToken.None);
                return false;
            }

            private JsonToken? GetEndToken(JContainer c)
            {
                switch (c.Type)
                {
                    case JTokenType.Object:
                        return JsonToken.EndObject;
                    case JTokenType.Array:
                        return JsonToken.EndArray;
                    case JTokenType.Constructor:
                        return JsonToken.EndConstructor;
                    case JTokenType.Property:
                        return null;
                    default:
                        throw new Exception("Unexpected JContainer type.");
                }
            }

            private bool ReadInto(JContainer c)
            {
                JToken firstChild = c.First;
                if (firstChild == null)
                {
                    return SetEnd(c);
                }
                else
                {
                    SetToken(firstChild);
                    _current = firstChild;
                    _parent = c;
                    return true;
                }
            }

            private bool SetEnd(JContainer c)
            {
                JsonToken? endToken = GetEndToken(c);
                if (endToken != null)
                {
                    SetToken(endToken.GetValueOrDefault());
                    _current = c;
                    _parent = c;
                    return true;
                }
                else
                {
                    return ReadOver(c);
                }
            }

            private void SetToken(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        SetToken(JsonToken.StartObject);
                        break;
                    case JTokenType.Array:
                        SetToken(JsonToken.StartArray);
                        break;
                    case JTokenType.Constructor:
                        SetToken(JsonToken.StartConstructor, ((JConstructor)token).Name);
                        break;
                    case JTokenType.Property:
                        SetToken(JsonToken.PropertyName, ((JProperty)token).Name);
                        break;
                    case JTokenType.Comment:
                        SetToken(JsonToken.Comment, ((JValue)token).Value);
                        break;
                    case JTokenType.Integer:
                        SetToken(JsonToken.Integer, ((JValue)token).Value);
                        break;
                    case JTokenType.Float:
                        SetToken(JsonToken.Float, ((JValue)token).Value);
                        break;
                    case JTokenType.String:
                        SetToken(JsonToken.String, ((JValue)token).Value);
                        break;
                    case JTokenType.Boolean:
                        SetToken(JsonToken.Boolean, ((JValue)token).Value);
                        break;
                    case JTokenType.Null:
                        SetToken(JsonToken.Null, ((JValue)token).Value);
                        break;
                    case JTokenType.Undefined:
                        SetToken(JsonToken.Undefined, ((JValue)token).Value);
                        break;
                    case JTokenType.Date:
                        SetToken(JsonToken.Date, ((JValue)token).Value);
                        break;
                    case JTokenType.Raw:
                        SetToken(JsonToken.Raw, ((JValue)token).Value);
                        break;
                    case JTokenType.Bytes:
                        SetToken(JsonToken.Bytes, ((JValue)token).Value);
                        break;
                    case JTokenType.Guid:
                        SetToken(JsonToken.String, SafeToString(((JValue)token).Value));
                        break;
                    case JTokenType.Uri:
                        object v = ((JValue)token).Value;
                        Uri uri = v as Uri;
                        SetToken(JsonToken.String, uri != null ? uri.OriginalString : SafeToString(v));
                        break;
                    case JTokenType.TimeSpan:
                        SetToken(JsonToken.String, SafeToString(((JValue)token).Value));
                        break;
                    default:
                        throw new Exception("Unexpected JTokenType.");
                }
            }

            private string SafeToString(object value)
            {
                return value?.ToString();
            }
        }
    }
}
