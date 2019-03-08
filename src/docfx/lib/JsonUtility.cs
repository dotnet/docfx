// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static readonly JsonMergeSettings s_mergeSettings = new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace };

        private static readonly JsonSerializer s_serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new JsonContractResolver { NamingStrategy = s_namingStrategy },
            Converters = { new StringEnumConverter { NamingStrategy = s_namingStrategy } },
        };

        private static readonly JsonSerializer s_schemaValidationSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new SchemaValidationContractResolver { NamingStrategy = s_namingStrategy },
            Converters = { new StringEnumConverter { NamingStrategy = s_namingStrategy } },
        };

        private static readonly JsonSerializer s_indentSerializer = new JsonSerializer
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new JsonContractResolver { NamingStrategy = s_namingStrategy },
            Converters = { new StringEnumConverter { NamingStrategy = s_namingStrategy } },
        };

        private static ThreadLocal<Stack<Status>> t_status = new ThreadLocal<Stack<Status>>(() => new Stack<Status>());

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
            if (schema == null)
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
        /// De-serialize a user input string to an object, return error list at the same time
        /// </summary>
        public static (List<Error> errors, T model) DeserializeWithSchemaValidation<T>(string json)
        {
            var (errors, token) = Deserialize(json);
            var (mismatchingErrors, result) = ToObjectWithSchemaValidation<T>(token);
            errors.AddRange(mismatchingErrors);
            return (errors, result);
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
                    var (range, message, path) = ParseException(ex);
                    throw Errors.JsonSyntaxError(range, message, path).ToException(ex);
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
        public static (List<Error>, T) ToObjectWithSchemaValidation<T>(JToken token)
        {
            var (errors, obj) = ToObjectWithSchemaValidation(token, typeof(T));
            return (errors, (T)obj);
        }

        /// <summary>
        /// Creates an instance of the specified .NET type from the JToken
        /// Schema validation errors will be ignored, type mismatch and syntax errors will be thrown
        /// </summary>
        public static T ToObject<T>(JToken token)
        {
            try
            {
                var obj = token.ToObject(typeof(T), JsonUtility.s_serializer);
                return (T)obj;
            }
            catch (JsonReaderException ex)
            {
                var (range, message, path) = ParseException(ex);
                throw Errors.JsonSyntaxError(range, message, path).ToException(ex);
            }
        }

        public static (List<Error>, object) ToObjectWithSchemaValidation(
            JToken token,
            Type type,
            Func<IEnumerable<DataTypeAttribute>, object, string, object> transform = null)
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
        public static (List<Error>, JToken) Deserialize(Document file, Context context) => context.Cache.LoadJsonFile(file);

        /// <summary>
        /// Parse a string to JToken.
        /// Validate null value during the process.
        /// </summary>
        public static (List<Error>, JToken) Deserialize(string json)
        {
            try
            {
                var (errors, token) = JToken.Parse(json).RemoveNulls();
                return (errors, token ?? JValue.CreateNull());
            }
            catch (JsonReaderException ex)
            {
                var (range, message, path) = ParseException(ex);
                throw Errors.JsonSyntaxError(range, message, path).ToException(ex);
            }
        }

        public static void Merge(JObject container, JObject overwrite)
        {
            container.Merge(overwrite, s_mergeSettings);
        }

        /// <summary>
        /// Report warnings for all null or undefined nodes, remove nulls inside arrays.
        /// </summary>
        public static (List<Error>, JToken) RemoveNulls(this JToken token)
        {
            var errors = new List<Error>();
            var nullNodes = new List<JToken>();
            var removeNodes = new List<JToken>();

            RemoveNullsCore(token, errors, nullNodes, removeNodes);

            foreach (var node in nullNodes)
            {
                var name = node is JProperty prop ? prop.Name : (node.Parent?.Parent is JProperty p ? p.Name : node.Path);
                errors.Add(Errors.NullValue(ToRange(node), name, node.Path));
            }

            foreach (var node in removeNodes)
            {
                node.Remove();
            }

            return (errors, token);
        }

        public static bool TryGetValue<T>(this JObject obj, string key, out T value) where T : JToken
        {
            value = null;
            if (obj == null || string.IsNullOrEmpty(key))
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

        private static Range ToRange(IJsonLineInfo lineInfo)
        {
            return lineInfo.HasLineInfo() ? new Range(lineInfo.LineNumber, lineInfo.LinePosition) : default;
        }

        private static void HandleError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
        {
            // only log an error once
            if (args.CurrentObject == args.ErrorContext.OriginalObject)
            {
                if (args.ErrorContext.Error is JsonReaderException || args.ErrorContext.Error is JsonSerializationException jse)
                {
                    var (range, message, path) = ParseException(args.ErrorContext.Error);
                    t_status.Value.Peek().Errors.Add(Errors.ViolateSchema(range, message, path));
                    args.ErrorContext.Handled = true;
                }
            }
        }

        private static (Range, string message, string path) ParseException(Exception ex)
        {
            // TODO: Json.NET type conversion error message is developer friendly but not writer friendly.
            var match = Regex.Match(ex.Message, "^([\\s\\S]*)\\sPath '(.*)', line (\\d+), position (\\d+).$");
            if (match.Success)
            {
                var range = new Range(int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value));
                return (range, match.Groups[1].Value, match.Groups[2].Value);
            }
            return (default, ex.Message, null);
        }

        private static bool IsNullOrUndefined(this JToken token)
        {
            return
                (token == null) ||
                (token.Type == JTokenType.Null) ||
                (token.Type == JTokenType.Undefined);
        }

        private static void RemoveNullsCore(JToken token, List<Error> errors, List<JToken> nullNodes, List<JToken> removeNodes)
        {
            if (token is JArray array)
            {
                foreach (var item in token.Children())
                {
                    if (item.IsNullOrUndefined())
                    {
                        nullNodes.Add(item);
                        removeNodes.Add(item);
                    }
                    else
                    {
                        RemoveNullsCore(item, errors, nullNodes, removeNodes);
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
                        RemoveNullsCore(prop.Value, errors, nullNodes, removeNodes);
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
                if (matchingProperty == null && type.IsSealed)
                {
                    errors.Add(Errors.UnknownField(ToRange(prop), prop.Name, type.Name, prop.Path));
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

        private class JsonContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                ShouldNotSerializeEmptyArray();
                SetFieldWritable();
                return prop;

                void ShouldNotSerializeEmptyArray()
                {
                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && !(prop.PropertyType == typeof(string)))
                    {
                        prop.ShouldSerialize =
                        target =>
                        {
                            var value = prop.ValueProvider.GetValue(target);

                            if (value is IEnumerable enumer && !enumer.GetEnumerator().MoveNext())
                            {
                                return false;
                            }

                            return true;
                        };
                    }
                }

                void SetFieldWritable()
                {
                    if (!prop.Writable)
                    {
                        if (member is FieldInfo f && f.IsPublic && !f.IsStatic)
                        {
                            prop.Writable = true;
                        }
                    }
                }
            }
        }

        private sealed class SchemaValidationContractResolver : JsonContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                var converter = GetConverter(member);

                if (converter != null)
                {
                    prop.Converter = converter;
                }
                return prop;
            }

            private SchemaValidationAndTransformConverter GetConverter(MemberInfo member)
            {
                var validators = member.GetCustomAttributes<ValidationAttribute>(false);
                var contentTypeAttributes = member.GetCustomAttributes<DataTypeAttribute>(false);
                if (contentTypeAttributes.Any() || validators.Any())
                {
                    return new SchemaValidationAndTransformConverter(contentTypeAttributes, validators, member.Name);
                }
                return null;
            }
        }

        private sealed class SchemaValidationAndTransformConverter : JsonConverter
        {
            private readonly IEnumerable<ValidationAttribute> _validators;
            private readonly IEnumerable<DataTypeAttribute> _attributes;
            private readonly string _fieldName;

            public SchemaValidationAndTransformConverter(IEnumerable<DataTypeAttribute> attributes, IEnumerable<ValidationAttribute> validators, string fieldName)
            {
                _attributes = attributes;
                _validators = validators;
                _fieldName = fieldName;
            }

            public override bool CanConvert(Type objectType) => true;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => new NotSupportedException();

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var range = ToRange((IJsonLineInfo)reader);
                var value = serializer.Deserialize(reader, objectType);
                if (value == null)
                {
                    return null;
                }

                foreach (var validator in _validators)
                {
                    try
                    {
                        validator.Validate(value, new ValidationContext(value) { DisplayName = _fieldName });
                    }
                    catch (Exception e)
                    {
                        t_status.Value.Peek().Errors.Add(Errors.ViolateSchema(range, e.Message, reader.Path));
                    }
                }

                var transform = t_status.Value.Peek().Transform;
                return transform != null ? transform(_attributes, value, reader.Path) : value;
            }
        }

        private sealed class Status
        {
            public List<Error> Errors { get; set; }

            public Func<IEnumerable<DataTypeAttribute>, object, string, object> Transform { get; set; }
        }
    }
}
