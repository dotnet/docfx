// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide Utilities of Json
    /// </summary>
    internal static class JsonUtility
    {
        public static readonly JsonSerializer DefaultDeserializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new JsonContractResolver(),
        };

        public static readonly JsonMergeSettings MergeSettings = new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
        };

        private static readonly JsonSerializerSettings s_noneFormatJsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters =
                {
                    new StringEnumConverter { CamelCaseText = true },
                },
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        private static readonly JsonSerializerSettings s_indentedFormatJsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters =
                {
                    new StringEnumConverter { CamelCaseText = true },
                },
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        private static readonly JsonSerializer s_defaultIndentedFormatSerializer = JsonSerializer.Create(s_indentedFormatJsonSerializerSettings);
        private static readonly JsonSerializer s_defaultNoneFormatSerializer = JsonSerializer.Create(s_noneFormatJsonSerializerSettings);

        [ThreadStatic]
        private static List<Error> t_schemaViolationErrors;

        [ThreadStatic]
        private static Func<DataTypeAttribute, object, object> t_transform;

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

        /// <summary>
        /// Serialize an object to TextWriter
        /// </summary>
        public static void Serialize(TextWriter writer, object graph, Formatting formatting = Formatting.None)
        {
            var localSerializer = formatting == Formatting.Indented ? s_defaultIndentedFormatSerializer : s_defaultNoneFormatSerializer;
            localSerializer.Serialize(writer, graph);
        }

        /// <summary>
        /// Serialize an object to string
        /// </summary>
        public static string Serialize(object graph, Formatting formatting = Formatting.None)
        {
            using (StringWriter writer = new StringWriter())
            {
                Serialize(writer, graph, formatting);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Deserialize a string to an object
        /// </summary>
        public static (List<Error>, T) Deserialize<T>(string json)
        {
            var (errors, token) = Deserialize(json);
            var (mismatchingErrors, result) = ToObject<T>(token);
            errors.AddRange(mismatchingErrors);
            return (errors, result);
        }

        /// <summary>
        /// Creates an instance of the specified .NET type from the JToken
        /// And validate mismatching field types
        /// </summary>
        public static (List<Error>, T) ToObject<T>(JToken token)
        {
            var (errors, obj) = ToObject(token, typeof(T));
            return (errors, (T)obj);
        }

        public static (List<Error>, object) ToObject(
            JToken token,
            Type type,
            Func<DataTypeAttribute, object, object> transform = null)
        {
            var errors = new List<Error>();
            try
            {
                t_transform = transform;
                t_schemaViolationErrors = new List<Error>();

                token.ReportUnknownFields(errors, type);
                var serializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = DefaultDeserializer.ContractResolver,
                };
                serializer.Error += HandleError;
                var value = token.ToObject(type, serializer);
                errors.AddRange(t_schemaViolationErrors);
                return (errors, value);
            }
            finally
            {
                t_transform = null;
                t_schemaViolationErrors = null;
            }

            void HandleError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                // only log an error once
                if (args.CurrentObject == args.ErrorContext.OriginalObject)
                {
                    if (args.ErrorContext.Error is JsonReaderException || args.ErrorContext.Error is JsonSerializationException jse)
                    {
                        var (range, message, path) = ParseException(args.ErrorContext.Error);
                        errors.Add(Errors.ViolateSchema(range, message, path));
                        args.ErrorContext.Handled = true;
                    }
                }
            }
        }

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

        public static JObject Merge(JObject a, JObject b)
        {
            var result = new JObject();
            result.Merge(a, MergeSettings);
            result.Merge(b, MergeSettings);
            return result;
        }

        public static JObject Merge(JObject first, IEnumerable<JObject> objs)
        {
            var result = new JObject();
            result.Merge(first);
            foreach (var obj in objs)
            {
                if (obj != null)
                {
                    result.Merge(obj, MergeSettings);
                }
            }
            return result;
        }

        public static (List<Error>, JToken) RemoveNulls(this JToken token)
        {
            var errors = new List<Error>();
            var nullNodes = new List<JToken>();
            token.FindNulls(errors, nullNodes);
            foreach (var node in nullNodes)
            {
                var lineInfo = (IJsonLineInfo)node;
                errors.Add(Errors.NullValue(new Range(lineInfo.LineNumber, lineInfo.LinePosition), node.Path));
                node.Remove();
            }
            return (errors, token);
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

        private static void FindNulls(this JToken token, List<Error> errors, List<JToken> nullNodes)
        {
            if (token is JArray array)
            {
                foreach (var item in token.Children())
                {
                    if (item.IsNullOrUndefined())
                    {
                        nullNodes.Add(item);
                    }
                    else
                    {
                        item.FindNulls(errors, nullNodes);
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
                        prop.Value.FindNulls(errors, nullNodes);
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
                    if (prop.Name.StartsWith('$'))
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
            var contract = DefaultDeserializer.ContractResolver.ResolveContract(type);
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
            var contract = DefaultDeserializer.ContractResolver.ResolveContract(type);

            if (contract is JsonObjectContract objectContract)
            {
                var matchingProperty = objectContract.Properties.GetClosestMatchProperty(prop.Name);
                if (matchingProperty == null && type.IsSealed)
                {
                    var lineInfo = prop as IJsonLineInfo;
                    errors.Add(Errors.UnknownField(
                        new Range(lineInfo.LineNumber, lineInfo.LinePosition), prop.Name, type.Name, prop.Path));
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
            var itemContract = DefaultDeserializer.ContractResolver.ResolveContract(arrayContract.CollectionItemType);
            if (itemContract is JsonObjectContract objectContract)
                return objectContract.Properties;
            else if (itemContract is JsonArrayContract contract)
                return GetPropertiesFromJsonArrayContract(contract);
            return null;
        }

        private static void SetFieldWritable(MemberInfo member, JsonProperty prop)
        {
            if (!prop.Writable)
            {
                if (member is FieldInfo f && f.IsPublic && !f.IsStatic)
                {
                    prop.Writable = true;
                }
            }
        }

        private sealed class JsonContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                var converter = GetConverter(member);

                if (converter != null)
                {
                    if (prop.PropertyType.IsArray)
                    {
                        prop.ItemConverter = converter;
                    }
                    else
                    {
                        prop.Converter = converter;
                    }
                }

                SetFieldWritable(member, prop);
                return prop;
            }

            private SchemaValidationAndTransformConverter GetConverter(MemberInfo member)
            {
                var validators = member.GetCustomAttributes<ValidationAttribute>(false);
                var contentTypeAttribute = member.GetCustomAttribute<DataTypeAttribute>();
                if (contentTypeAttribute != null || validators.Any())
                {
                    return new SchemaValidationAndTransformConverter(contentTypeAttribute, validators, member.Name);
                }
                return null;
            }
        }

        private sealed class SchemaValidationAndTransformConverter : JsonConverter
        {
            private readonly IEnumerable<ValidationAttribute> _validators;
            private readonly DataTypeAttribute _attribute;
            private readonly string _fieldName;

            public SchemaValidationAndTransformConverter(DataTypeAttribute attribute, IEnumerable<ValidationAttribute> validators, string fieldName)
            {
                _attribute = attribute;
                _validators = validators;
                _fieldName = fieldName;
            }

            public override bool CanConvert(Type objectType) => true;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => new NotSupportedException();

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var lineInfo = (IJsonLineInfo)reader;
                var range = new Range(lineInfo.LineNumber, lineInfo.LinePosition);
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
                        t_schemaViolationErrors.Add(Errors.ViolateSchema(range, e.Message, reader.Path));
                    }
                }

                return t_transform != null ? t_transform(_attribute, value) : value;
            }
        }
    }
}
