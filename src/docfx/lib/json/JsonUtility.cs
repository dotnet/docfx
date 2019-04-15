// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        private static readonly NamingStrategy s_namingStrategy = new CamelCaseNamingStrategy();
        private static readonly JsonConverter[] s_jsonConverters =
        {
            new StringEnumConverter { NamingStrategy = s_namingStrategy },
            new SourceInfoJsonConverter { },
        };

        private static readonly JsonSerializer s_serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = s_jsonConverters,
            ContractResolver = new JsonContractResolver { NamingStrategy = s_namingStrategy },
        });

        private static readonly JsonSerializer s_schemaValidationSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = s_jsonConverters,
            ContractResolver = new SchemaContractResolver { NamingStrategy = s_namingStrategy },
        });

        private static readonly JsonSerializer s_indentSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = s_jsonConverters,
            ContractResolver = new JsonContractResolver { NamingStrategy = s_namingStrategy },
        });

        private static readonly ThreadLocal<Stack<Status>> t_status = new ThreadLocal<Stack<Status>>(() => new Stack<Status>());

        internal static JsonSerializer Serializer => s_serializer;

        internal static Status State => t_status.Value.Peek();

        static JsonUtility()
        {
            s_schemaValidationSerializer.Error += HandleError;
        }

        /// <summary>
        /// Fast pass to read MIME from $schema attribute.
        /// </summary>
        public static string ReadMime(TextReader reader)
        {
            var schema = ReadSchema(reader);
            if (schema is null)
                return null;

            // TODO: be more strict
            var mime = schema.Split('/').LastOrDefault();
            if (mime != null)
                return Path.GetFileNameWithoutExtension(schema);

            return null;
        }

        /// <summary>
        /// Fast pass to read the value of $schema specified in JSON.
        /// $schema must be the first attribute in the root object.
        /// Assume input is a valid JSON. Bad input will be process though Json.NET
        /// </summary>
        public static string ReadSchema(TextReader reader)
        {
            SkipSpaces();

            if (reader.Read() != '{')
                return null;

            SkipSpaces();

            foreach (var expect in "\"$schema\"")
            {
                if (reader.Read() != expect)
                    return null;
            }

            SkipSpaces();

            if (reader.Read() != ':')
                return null;

            SkipSpaces();

            if (reader.Peek() != '\"')
                return null;

            return new JsonTextReader(reader).ReadAsString();

            void SkipSpaces()
            {
                while (true)
                {
                    var ch = reader.Peek();
                    if (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t')
                    {
                        reader.Read();
                        continue;
                    }
                    break;
                }
            }
        }

        public static IEnumerable<string> GetPropertyNames(Type type)
        {
            return ((JsonObjectContract)s_serializer.ContractResolver.ResolveContract(type)).Properties.Select(prop => prop.PropertyName);
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
            using (StringWriter writer = new StringWriter())
            {
                Serialize(writer, graph, indent);
                return writer.ToString();
            }
        }

        /// <summary>
        /// De-serialize a data string, which is not user input, to an object
        /// schema validation errors will be ignored, syntax errors and type mismatching will be thrown
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var reader = new JsonTextReader(stringReader))
            {
                try
                {
                    return s_serializer.Deserialize<T>(reader);
                }
                catch (JsonReaderException ex)
                {
                    var (source, message) = ParseException(ex);
                    throw Errors.JsonSyntaxError(source, message).ToException(ex);
                }
            }
        }

        /// <summary>
        /// Converts a strongly typed C# object to weakly typed json object using the default serialization settings.
        /// </summary>
        public static JObject ToJObject(object model)
        {
            return JObject.FromObject(model, s_serializer);
        }

        /// <summary>
        /// Creates an instance of the specified .NET type from the JToken with schema validation
        /// </summary>
        public static (List<Error>, T) ToObject<T>(JToken token)
        {
            var (errors, obj) = ToObject(token, typeof(T));
            return (errors, (T)obj);
        }

        public static (List<Error>, object) ToObject(
            JToken token,
            Type type,
            Func<IEnumerable<DataTypeAttribute>, SourceInfo<object>, string, object> transform = null)
        {
            try
            {
                var errors = new List<Error>();
                var status = new Status { Errors = errors, Transform = transform };

                t_status.Value.Push(status);

                token.ReportUnknownFields(errors, type);

                var value = token.ToObject(type, s_schemaValidationSerializer);

                return (errors, value);
            }
            finally
            {
                t_status.Value.Pop();
            }
        }

        /// <summary>
        /// Deserialize from JSON file, get from or add to cache
        /// </summary>
        public static (List<Error>, JToken) Parse(Document file, Context context) => context.Cache.LoadJsonFile(file);

        /// <summary>
        /// Parse a string to JToken.
        /// Validate null value during the process.
        /// </summary>
        public static (List<Error>, JToken) Parse(string json, string file)
        {
            try
            {
                return SetSourceInfo(JToken.Parse(json), file).RemoveNulls();
            }
            catch (JsonReaderException ex)
            {
                var (source, message) = ParseException(ex);
                throw Errors.JsonSyntaxError(source, message).ToException(ex);
            }
        }

        public static void Merge(JObject container, JObject overwrite)
        {
            if (overwrite is null)
                return;

            foreach (var property in overwrite.Properties())
            {
                var key = property.Name;
                var value = property.Value;

                if (container[key] is JObject containerObj && value is JObject overwriteObj)
                {
                    Merge(containerObj, overwriteObj);
                }
                else if (IsNullOrUndefined(container[key]) || !IsNullOrUndefined(value))
                {
                    container[key] = SetSourceInfo(DeepClone(value), value.Annotation<SourceInfo>());
                    SetSourceInfo(container.Property(key), property.Annotation<SourceInfo>());
                }
            }

            JToken DeepClone(JToken token)
            {
                if (token is JValue v)
                {
                    return SetSourceInfo(new JValue(v), token.Annotation<SourceInfo>());
                }

                if (token is JObject obj)
                {
                    var result = new JObject();
                    foreach (var prop in obj.Properties())
                    {
                        result[prop.Name] = SetSourceInfo(DeepClone(prop.Value), prop.Value.Annotation<SourceInfo>());
                        SetSourceInfo(result.Property(prop.Name), prop.Annotation<SourceInfo>());
                    }
                    return SetSourceInfo(result, token.Annotation<SourceInfo>());
                }

                if (token is JArray array)
                {
                    var result = new JArray();
                    foreach (var item in array)
                    {
                        result.Add(SetSourceInfo(DeepClone(item), item.Annotation<SourceInfo>()));
                    }
                    return SetSourceInfo(result, token.Annotation<SourceInfo>());
                }

                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Trims all string values
        /// </summary>
        public static void TrimStringValues(JToken token)
        {
            switch (token)
            {
                case JValue scalar when scalar.Value is string str:
                    scalar.Value = str.Trim();
                    break;

                case JArray array:
                    foreach (var item in array)
                    {
                        TrimStringValues(item);
                    }
                    break;

                case JObject map:
                    foreach (var (key, value) in map)
                    {
                        TrimStringValues(value);
                    }
                    break;
            }
        }

        /// <summary>
        /// Report warnings for all null or undefined nodes, remove nulls inside arrays.
        /// </summary>
        public static (List<Error>, JToken) RemoveNulls(this JToken token)
        {
            var errors = new List<Error>();
            var nullNodes = new List<JToken>();
            var nullArrayNodes = new List<JToken>();

            RemoveNullsCore(token, errors, nullNodes, nullArrayNodes);

            foreach (var node in nullNodes)
            {
                var (lineInfo, name) = Parse(node);
                errors.Add(Errors.NullValue(GetSourceInfo(node), name));
            }

            foreach (var node in nullArrayNodes)
            {
                var (lineInfo, name) = Parse(node);
                errors.Add(Errors.NullArrayValue(new SourceInfo(null, lineInfo.LineNumber, lineInfo.LinePosition), name));
                node.Remove();
            }

            return (errors, token);

            (IJsonLineInfo lineInfo, string name) Parse(JToken node)
            {
                var lineInfo = (IJsonLineInfo)node;
                var name = node is JProperty prop ? prop.Name : (node.Parent?.Parent is JProperty p ? p.Name : node.Path);
                return (lineInfo, name);
            }
        }

        public static bool TryGetValue<T>(this JObject obj, string key, out T value) where T : JToken
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

        public static SourceInfo GetSourceInfo(JToken token)
        {
            return token.Annotation<SourceInfo>();
        }

        internal static JToken SetSourceInfo(JToken token, SourceInfo source)
        {
            token.AddAnnotation(source ?? SourceInfo.Empty);
            if (source != null)
            {
                s_setLineInfo(token, source.Line, source.Column);
            }
            return token;
        }

        private static JToken SetSourceInfo(JToken token, string file)
        {
            var lineInfo = (IJsonLineInfo)token;
            token.AddAnnotation(new SourceInfo(file, lineInfo.LineNumber, lineInfo.LinePosition));

            switch (token)
            {
                case JProperty prop:
                    SetSourceInfo(prop.Value, file);
                    break;

                case JArray arr:
                    foreach (var item in arr)
                    {
                        SetSourceInfo(item, file);
                    }
                    break;

                case JObject obj:
                    foreach (var prop in obj.Properties())
                    {
                        SetSourceInfo(prop, file);
                    }
                    break;
            }

            return token;
        }

        private static void HandleError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
        {
            // only log an error once
            if (args.CurrentObject == args.ErrorContext.OriginalObject)
            {
                if (args.ErrorContext.Error is JsonReaderException || args.ErrorContext.Error is JsonSerializationException jse)
                {
                    var (source, message) = ParseException(args.ErrorContext.Error);
                    t_status.Value.Peek().Errors.Add(Errors.ViolateSchema(source, message));
                    args.ErrorContext.Handled = true;
                }
            }
        }

        private static (SourceInfo, string message) ParseException(Exception ex)
        {
            // TODO: Json.NET type conversion error message is developer friendly but not writer friendly.
            var match = Regex.Match(ex.Message, "^([\\s\\S]*)\\sPath '(.*)', line (\\d+), position (\\d+).$");
            if (match.Success)
            {
                var source = new SourceInfo(null, int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value));
                return (source, RewriteErrorMessage(match.Groups[1].Value));
            }

            match = Regex.Match(ex.Message, "^([\\s\\S]*)\\sPath '(.*)'.$");
            if (match.Success)
            {
                return (default, RewriteErrorMessage(match.Groups[1].Value));
            }
            return (default, RewriteErrorMessage(ex.Message));
        }

        private static string RewriteErrorMessage(string message)
        {
            if (message.StartsWith("Error reading string. Unexpected token"))
            {
                return "Expected type String, please input String or type compatible with String.";
            }
            return message;
        }

        private static bool IsNullOrUndefined(this JToken token)
        {
            return
                (token is null) ||
                (token.Type == JTokenType.Null) ||
                (token.Type == JTokenType.Undefined);
        }

        private static void RemoveNullsCore(JToken token, List<Error> errors, List<JToken> nullNodes, List<JToken> nullArrayNodes)
        {
            if (token is JArray array)
            {
                foreach (var item in token.Children())
                {
                    if (item.IsNullOrUndefined())
                    {
                        nullArrayNodes.Add(item);
                    }
                    else
                    {
                        RemoveNullsCore(item, errors, nullNodes, nullArrayNodes);
                    }
                }
            }
            else if (token is JObject obj)
            {
                foreach (var item in token.Children())
                {
                    var prop = item as JProperty;
                    if (prop.Value.IsNullOrUndefined())
                    {
                        nullNodes.Add(item);
                    }
                    else
                    {
                        RemoveNullsCore(prop.Value, errors, nullNodes, nullArrayNodes);
                    }
                }
            }
        }

        private static void ReportUnknownFields(this JToken token, List<Error> errors, Type type)
        {
            if (token is JArray array)
            {
                var itemType = GetCollectionItemTypeIfArrayType(type);
                foreach (var item in token.Children())
                {
                    item.ReportUnknownFields(errors, itemType);
                }
            }
            else if (token is JObject obj)
            {
                foreach (var item in token.Children())
                {
                    var prop = item as JProperty;

                    // skip the special property
                    if (prop.Name == "$schema")
                        continue;

                    var nestedType = GetNestedTypeAndCheckForUnknownField(type, prop, errors);
                    if (nestedType != null)
                    {
                        prop.Value.ReportUnknownFields(errors, nestedType);
                    }
                }
            }
        }

        private static Type GetCollectionItemTypeIfArrayType(Type type)
        {
            var contract = s_serializer.ContractResolver.ResolveContract(type);
            if (contract is JsonObjectContract)
            {
                return type;
            }
            else if (contract is JsonArrayContract arrayContract)
            {
                var itemType = arrayContract.CollectionItemType;
                if (itemType is null)
                {
                    return type;
                }
                else
                {
                    return itemType;
                }
            }
            return type;
        }

        private static Type GetNestedTypeAndCheckForUnknownField(Type type, JProperty prop, List<Error> errors)
        {
            var contract = s_serializer.ContractResolver.ResolveContract(type);

            if (contract is JsonObjectContract objectContract)
            {
                var matchingProperty = objectContract.Properties.GetClosestMatchProperty(prop.Name);
                if (matchingProperty is null && type.IsSealed)
                {
                    errors.Add(Errors.UnknownField(GetSourceInfo(prop), prop.Name, type.Name));
                }
                return matchingProperty?.PropertyType;
            }

            if (contract is JsonArrayContract arrayContract)
            {
                var matchingProperty = GetPropertiesFromJsonArrayContract(arrayContract).GetClosestMatchProperty(prop.Name);
                return matchingProperty?.PropertyType;
            }

            return null;
        }

        private static JsonPropertyCollection GetPropertiesFromJsonArrayContract(JsonArrayContract arrayContract)
        {
            var itemContract = s_serializer.ContractResolver.ResolveContract(arrayContract.CollectionItemType);
            if (itemContract is JsonObjectContract objectContract)
                return objectContract.Properties;
            else if (itemContract is JsonArrayContract contract)
                return GetPropertiesFromJsonArrayContract(contract);
            return null;
        }

        internal class Status
        {
            public List<Error> Errors { get; set; }

            public Func<IEnumerable<DataTypeAttribute>, SourceInfo<object>, string, object> Transform { get; set; }
        }
    }
}
