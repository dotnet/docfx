// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    internal static class JsonUtility
    {
        public static readonly JTokenDeepEqualsComparer DeepEqualsComparer = new JTokenDeepEqualsComparer();

        private static readonly NamingStrategy s_namingStrategy = new CamelCaseNamingStrategy();
        private static readonly JsonConverter[] s_jsonConverters =
        {
            new StringEnumConverter { NamingStrategy = s_namingStrategy },
            new SourceInfoJsonConverter { },
            new JTokenJsonConverter { },
        };

        private static readonly JsonSerializer s_serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = s_jsonConverters,
            ContractResolver = new JsonContractResolver { NamingStrategy = s_namingStrategy },
        });

        private static readonly JsonSerializer s_serializerCheckingAdditional = JsonSerializer.Create(new JsonSerializerSettings
        {
            CheckAdditionalContent = true,
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = s_jsonConverters,
            ContractResolver = new JsonContractResolver { NamingStrategy = s_namingStrategy },
        });

        private static readonly JsonSerializer s_schemaValidationSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            Converters = s_jsonConverters,
            ContractResolver = new JsonContractResolver { NamingStrategy = s_namingStrategy },
        });

        private static readonly JsonSerializer s_indentSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            Converters = s_jsonConverters,
            ContractResolver = new JsonContractResolver { NamingStrategy = s_namingStrategy },
        });

        private static readonly ThreadLocal<Stack<Status>> t_status = new ThreadLocal<Stack<Status>>(() => new Stack<Status>());

        internal static JsonSerializer Serializer => s_serializer;

        internal static Status? State => t_status.Value!.TryPeek(out var result) ? result : null;

        static JsonUtility()
        {
            s_schemaValidationSerializer.Error += HandleError;
        }

        /// <summary>
        /// Fast pass to read MIME from $schema attribute.
        /// </summary>
        public static SourceInfo<string?> ReadMime(TextReader reader, FilePath file)
        {
            var schema = ReadSchema(reader, file);
            if (schema.Value is null)
            {
                return schema;
            }

            // TODO: be more strict
            var mime = schema.Value.Split('/').LastOrDefault();
            return new SourceInfo<string?>(mime != null ? Path.GetFileNameWithoutExtension(schema) : null, schema.Source);
        }

        public static IEnumerable<string> GetPropertyNames(Type type)
        {
            return
                from prop in ((JsonObjectContract)s_serializer.ContractResolver.ResolveContract(type)).Properties
                where !string.IsNullOrEmpty(prop.PropertyName)
                select prop.PropertyName;
        }

        /// <summary>
        /// Serialize an object to TextWriter
        /// </summary>
        public static void Serialize(TextWriter writer, object graph, bool indent = false)
        {
            var serializer = indent ? s_indentSerializer : s_serializer;
            serializer.Serialize(writer, graph);
        }

        /// <summary>
        /// Serialize an object to string
        /// </summary>
        public static string Serialize(object graph, bool indent = false)
        {
            using var writer = new StringWriter();
            Serialize(writer, graph, indent);
            return writer.ToString();
        }

        /// <summary>
        /// De-serialize a data string, which is not user input, to an object
        /// schema validation errors will be ignored, syntax errors and type mismatching will be thrown
        /// </summary>
        public static T DeserializeData<T>(string data, FilePath? file) where T : class, new()
        {
            using var reader = new StringReader(data);
            return DeserializeData<T>(reader, file, true);
        }

        /// <summary>
        /// De-serialize a data string, which is not user input, to an object
        /// schema validation errors will be ignored, syntax errors and type mismatching will be thrown
        /// </summary>
        public static T DeserializeData<T>(TextReader data, FilePath? file, bool checkAdditionalContent = true) where T : class, new()
        {
            using var reader = new JsonTextReader(data);
            try
            {
                var status = new Status { FilePath = file };

                t_status.Value!.Push(status);

                return (checkAdditionalContent
                    ? s_serializerCheckingAdditional.Deserialize<T>(reader)
                    : s_serializer.Deserialize<T>(reader))
                    ?? new T();
            }
            catch (JsonReaderException ex)
            {
                throw ToError(ex, file).ToException(ex);
            }
            catch (JsonSerializationException ex)
            {
                throw ToError(ex, file).ToException(ex);
            }
            finally
            {
                t_status.Value!.Pop();
            }
        }

        /// <summary>
        /// Converts a strongly typed C# object to weakly typed json object using the default serialization settings.
        /// </summary>
        public static JObject ToJObject(object model)
        {
            return JObject.FromObject(model, s_serializer);
        }

        public static T Deserialize<T>(ErrorBuilder errors, string json, FilePath file) where T : class, new()
        {
            using var reader = new StringReader(json);
            return Deserialize<T>(errors, reader, file);
        }

        public static T Deserialize<T>(ErrorBuilder errors, TextReader reader, FilePath file) where T : class, new()
        {
            return ToObject<T>(errors, Parse(errors, reader, file));
        }

        /// <summary>
        /// Creates an instance of the specified .NET type from the JToken with schema validation
        /// </summary>
        public static T ToObject<T>(ErrorBuilder errors, JToken token) where T : class, new()
        {
            return ToObject(errors, token, typeof(T)) as T ?? new T();
        }

        public static object? ToObject(ErrorBuilder errors, JToken token, Type type)
        {
            try
            {
                var status = new Status { Reader = new JTokenReader(token) };
                t_status.Value!.Push(status);

                var value = s_schemaValidationSerializer.Deserialize(status.Reader, type);
                errors.AddRange(status.Errors);
                return value;
            }
            finally
            {
                t_status.Value!.Pop();
            }
        }

        /// <summary>
        /// Parse a string to JToken.
        /// Validate null value during the process.
        /// </summary>
        public static JToken Parse(ErrorBuilder errors, string json, FilePath file)
        {
            return Parse(errors, new StringReader(json), file);
        }

        public static JToken Parse(ErrorBuilder errors, TextReader json, FilePath file)
        {
            try
            {
                using var reader = new JsonTextReader(json) { DateParseHandling = DateParseHandling.None };
                return SetSourceInfo(JToken.ReadFrom(reader), file).RemoveNulls(errors);
            }
            catch (JsonReaderException ex)
            {
                throw ToError(ex, file).ToException(ex);
            }
        }

        public static string? AppendPropertyName(string? path, string name)
        {
            return string.IsNullOrEmpty(path) ? name : string.Concat(path, ".", name);
        }

        public static void Merge(JObject container, params JObject[] overwrites)
        {
            Merge(Array.Empty<string>(), container, overwrites);
        }

        public static void Merge(string[] unionProperties, JObject container, params JObject?[] overwrites)
        {
            foreach (var overwrite in overwrites)
            {
                Merge(container, overwrite, unionProperties);
            }
        }

        public static void Merge(JObject container, JObject? overwrite, string[]? unionProperties = null, string? rebase = null)
        {
            if (overwrite is null)
            {
                return;
            }

            foreach (var (key, value) in overwrite)
            {
                if (value is null)
                {
                    continue;
                }
                else if (container[key] is JObject containerObj && value is JObject overwriteObj)
                {
                    Merge(containerObj, overwriteObj, unionProperties, AppendPropertyName(rebase, key));
                }
                else if (container[key] is JArray array && value is JArray newArray && unionProperties?.Contains(key) == true)
                {
                    // TODO: need to check if miss line info for JArray
                    SetProperty(container, key, new JArray(newArray.Union(array)), rebase);
                }
                else
                {
                    SetProperty(container, key, value, rebase);
                }
            }
        }

        public static JToken DeepClone(JToken? token)
        {
            return DeepCloneCore(token, rebase: null);
        }

        public static void AddRange(this JArray container, IEnumerable arr)
        {
            foreach (var item in arr)
            {
                container.Add(item);
            }
        }

        /// <summary>
        /// Sets the property value. Prefer this method when you need to propagate source info.
        /// </summary>
        public static void SetProperty(this JObject obj, string key, JToken value, string? rebase = null)
        {
            // Assign a JToken to a property erases line info,
            // See https://github.com/JamesNK/Newtonsoft.Json/issues/2055.
            var newValue = DeepCloneCore(value, AppendPropertyName(rebase, key));

            obj[key] = newValue;

            // Json.NET reuses existing property when value equals,
            // in that case, value source info will not be assigned to the property.
            var property = obj.Property(key);
            if (property != null)
            {
                SetSourceInfo(property.Value, newValue.GetSourceInfo());
            }
        }

        /// <summary>
        /// Report warnings for null values inside arrays and remove nulls inside arrays.
        /// </summary>
        public static JToken RemoveNulls(this JToken root, ErrorBuilder errors)
        {
            var nullArrayNodes = new List<(JToken, string)>();

            RemoveNullsCore(root, null);

            foreach (var (node, name) in nullArrayNodes)
            {
                errors.Add(Errors.Json.NullArrayValue(GetSourceInfo(node), name));
                node.Remove();
            }

            // treat null JToken as empty JObject since it is from user input
            return IsNullOrUndefined(root) ? new JObject() : root;

            void RemoveNullsCore(JToken token, string? name)
            {
                if (token is JArray array)
                {
                    foreach (var item in array)
                    {
                        if (item.IsNullOrUndefined())
                        {
                            nullArrayNodes.Add((item, name ?? item.Path));
                        }
                        else
                        {
                            RemoveNullsCore(item, name);
                        }
                    }
                }
                else if (token is JObject obj)
                {
                    foreach (var (key, value) in obj)
                    {
                        if (!value.IsNullOrUndefined())
                        {
                            RemoveNullsCore(value, key);
                        }
                    }
                }
            }
        }

        public static bool TryGetValue<T>(this JObject obj, string key, [NotNullWhen(true)] out T? value) where T : JToken
        {
            value = null;
            if (obj is null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (obj.TryGetValue(key, out var valueToken) && valueToken is T valueT)
            {
                value = valueT;
                return true;
            }

            return false;
        }

        public static SourceInfo? GetSourceInfo(this JToken token)
        {
            var result = token.Annotation<SourceInfo>();

            // When JObject is used as JsonExtensionData, it is hard to populate source info
            // of the key to JProperty due to the limitation of ExtensionDataSetter interface,
            // thus fallback to use value source info.
            if (result is null && token is JProperty property)
            {
                return property.Value.Annotation<SourceInfo>();
            }
            return result;
        }

        public static JToken SetSourceInfo(JToken token, SourceInfo? source)
        {
            token.RemoveAnnotations<SourceInfo>();
            if (source != null)
            {
                token.AddAnnotation(source);
            }
            return token;
        }

        public static SourceInfo? GetKeySourceInfo(JToken token)
        {
            return token.Annotation<SourceInfo>()?.KeySourceInfo;
        }

        public static JObject SortProperties(JObject obj)
        {
            var properties = new SortedList<string, JProperty>();
            foreach (var property in obj.Properties())
            {
                properties.Add(property.Name, !(property.Value is JObject childObj) ? property : new JProperty(property.Name, SortProperties(childObj)));
            }

            return new JObject(properties.Values);
        }

        internal static void SkipToken(JsonReader reader)
        {
            var currentDepth = reader.Depth;
            reader.Skip();
            while (reader.Depth > currentDepth)
            {
                if (!reader.Read())
                {
                    break;
                }
            }
        }

        private static JToken DeepCloneCore(JToken? token, string? rebase)
        {
            if (token is JValue v)
            {
                return SetSourceInfo(new JValue(v), GetSourceInfo(v, rebase));
            }

            if (token is JObject obj)
            {
                var result = new JObject();
                foreach (var (key, value) in obj)
                {
                    result[key] = DeepCloneCore(value, rebase is null ? rebase : AppendPropertyName(rebase, key));
                }
                return SetSourceInfo(result, GetSourceInfo(obj, rebase));
            }

            if (token is JArray array)
            {
                var result = new JArray();
                foreach (var item in array)
                {
                    result.Add(DeepCloneCore(item, rebase));
                }
                return SetSourceInfo(result, GetSourceInfo(array, rebase));
            }

            throw new NotSupportedException();

            static SourceInfo? GetSourceInfo(JToken token, string? rebase)
            {
                var sourceInfo = token.Annotation<SourceInfo>();
                return sourceInfo != null && rebase != null ? sourceInfo.WithPropertyPath(rebase) : sourceInfo;
            }
        }

        /// <summary>
        /// Fast pass to read the value of $schema specified in JSON.
        /// $schema must be the first attribute in the root object.
        /// Assume input is a valid JSON. Bad input will be processed through Json.NET
        /// </summary>
        private static SourceInfo<string?> ReadSchema(TextReader reader, FilePath file)
        {
            try
            {
                using var json = new JsonTextReader(reader);
                if (json.Read() && json.TokenType == JsonToken.StartObject)
                {
                    if (json.Read() && json.TokenType == JsonToken.PropertyName && json.Value is string str && str == "$schema")
                    {
                        if (json.Read() && json.Value is string schema)
                        {
                            var lineInfo = (IJsonLineInfo)json;
                            return new SourceInfo<string?>(schema, new SourceInfo(file, lineInfo.LineNumber, lineInfo.LinePosition));
                        }
                    }
                }
                return new SourceInfo<string?>(null, new SourceInfo(file, 1, 1));
            }
            catch (JsonReaderException)
            {
                return default;
            }
        }

        private static bool IsNullOrUndefined([NotNullWhen(false)] this JToken? token)
        {
            return
                (token is null) ||
                (token.Type == JTokenType.Null) ||
                (token.Type == JTokenType.Undefined);
        }

        private static JToken SetSourceInfo(JToken token, FilePath file, string? path = null, SourceInfo? keySourceInfo = null)
        {
            var lineInfo = (IJsonLineInfo)token;
            var sourceInfo = new SourceInfo(file, lineInfo.LineNumber, lineInfo.LinePosition, path, keySourceInfo);
            SetSourceInfo(token, sourceInfo);

            switch (token)
            {
                case JProperty prop:
                    SetSourceInfo(prop.Value, file, path);
                    break;

                case JArray arr:
                    foreach (var item in arr)
                    {
                        SetSourceInfo(item, file, path);
                    }
                    break;

                case JObject obj:
                    foreach (var property in obj.Properties())
                    {
                        var keyLineInfo = (IJsonLineInfo)property;
                        var keySource = new SourceInfo(file, keyLineInfo.LineNumber, keyLineInfo.LinePosition, path);
                        SetSourceInfo(property.Value, file, AppendPropertyName(path, property.Name), keySource);
                    }
                    break;
            }

            return token;
        }

        private static void HandleError(object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs? args)
        {
            // only log an error once
            if (args?.CurrentObject == args?.ErrorContext.OriginalObject)
            {
                if (args?.ErrorContext.Error is JsonReaderException || args?.ErrorContext.Error is JsonSerializationException)
                {
                    var state = t_status.Value!.Peek();
                    state.Errors.Add(Errors.Json.ViolateSchema(state.Reader?.CurrentToken?.GetSourceInfo(), ParseException(args.ErrorContext.Error).message));
                    args.ErrorContext.Handled = true;
                }
            }
        }

        private static Error ToError(Exception ex, FilePath? file)
        {
            var (message, line, column) = ParseException(ex);

            return Errors.Json.JsonSyntaxError(file is null ? null : new SourceInfo(file, line, column), message);
        }

        private static (string message, int line, int column) ParseException(Exception ex)
        {
            // TODO: Json.NET type conversion error message is developer friendly but not writer friendly.
            var match = Regex.Match(ex.Message, "^([\\s\\S]*)\\sPath '(.*)', line (\\d+), position (\\d+).$");
            if (match.Success)
            {
                return (RewriteErrorMessage(match.Groups[1].Value), int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value));
            }

            match = Regex.Match(ex.Message, "^([\\s\\S]*)\\sPath '(.*)'.$");

            return (RewriteErrorMessage(match.Success ? match.Groups[1].Value : ex.Message), 0, 0);
        }

        private static string RewriteErrorMessage(string message)
        {
            if (message.StartsWith("Error reading string. Unexpected token") || message.Contains("into type 'System.String'"))
            {
                return "Expected type String, please input String or type compatible with String.";
            }
            return message;
        }

        internal class Status
        {
            public List<Error> Errors { get; } = new List<Error>();

            public FilePath? FilePath { get; set; }

            public JTokenReader? Reader { get; set; }
        }
    }
}
