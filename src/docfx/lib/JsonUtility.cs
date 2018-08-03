// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
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

        private static readonly ConcurrentDictionary<Type, Lazy<bool>> s_cacheTypeContainsJsonExtensionData = new ConcurrentDictionary<Type, Lazy<bool>>();
        private static readonly ConcurrentDictionary<Type, Lazy<JSchema>> s_cacheTypeJsonSchema = new ConcurrentDictionary<Type, Lazy<JSchema>>();

        private static readonly JsonMergeSettings s_defaultMergeSettings = new JsonMergeSettings
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

        public static (List<Error>, object) ToObject(JToken token, Type type)
        {
            try
            {
                return (token.ValidateMismatchingFieldType(type), token.ToObject(type, DefaultDeserializer));
            }
            catch (JsonException ex) when (ex is JsonSerializationException || ex is JsonReaderException)
            {
                var (message, range) = ParseRangeFromExceptionMessage(ex.Message);
                throw Errors.ViolateSchema(range, message).ToException();
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
                var (errors, token) = JToken.Parse(json, new JsonLoadSettings { LineInfoHandling = LineInfoHandling.Load })
                    .ValidateNullValue();
                return (errors, token ?? JValue.CreateNull());
            }
            catch (Exception ex)
            {
                throw Errors.JsonSyntaxError(ex).ToException();
            }
        }

        /// <summary>
        /// Merge multiple JSON objects.
        /// The latter value overwrites the former value for a given key.
        /// </summary>
        public static JObject Merge(params JObject[] objs)
        {
            var result = new JObject();
            foreach (var obj in objs)
            {
                if (obj != null)
                {
                    result.Merge(obj, s_defaultMergeSettings);
                }
            }
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
                    result.Merge(obj, s_defaultMergeSettings);
                }
            }
            return result;
        }

        internal static (List<Error>, JToken) ValidateNullValue(this JToken token)
        {
            var errors = new List<Error>();
            var nullNodes = new List<JToken>();
            token.TraverseForNullValueValidation(errors, nullNodes);
            foreach (var node in nullNodes)
            {
                node.Remove();
            }
            return (errors, token);
        }

        internal static List<Error> ValidateMismatchingFieldType(this JToken token, Type type)
        {
            var errors = new List<Error>();
            token.TraverseForUnknownFieldType(errors, type);
            return errors;
        }

        internal static JSchema GetJsonSchemaFromType(Type type)
        {
            return s_cacheTypeJsonSchema.GetOrAdd(
                type,
                new Lazy<JSchema>(() =>
                {
                    var generator = new JSchemaGenerator() { ContractResolver = DefaultDeserializer.ContractResolver, DefaultRequired = Required.DisallowNull };
                    return generator.Generate(type, true);
                })).Value;
        }

        private static (string, Range) ParseRangeFromExceptionMessage(string message)
        {
            var parts = message.Remove(message.Length - 1).Split(',');
            var lineNumber = int.Parse(parts.SkipLast(1).Last().Split(' ').Last());
            var linePosition = int.Parse(parts.Last().Split(' ').Last());
            return (message.Substring(0, message.IndexOf(".")), new Range(lineNumber, linePosition));
        }

        private static bool IsNullOrUndefined(this JToken token)
        {
            return
                (token == null) ||
                (token.Type == JTokenType.Null) ||
                (token.Type == JTokenType.Undefined);
        }

        private static void TraverseForNullValueValidation(this JToken token, List<Error> errors, List<JToken> nullNodes, string name = null)
        {
            if (token is JArray array)
            {
                foreach (var item in token.Children())
                {
                    if (item.IsNullOrUndefined())
                    {
                        LogInfoForNullValue(array, errors, name);
                        nullNodes.Add(item);
                    }
                    else
                    {
                        item.TraverseForNullValueValidation(errors, nullNodes, name);
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
                        LogInfoForNullValue(token, errors, prop.Name);
                        nullNodes.Add(item);
                    }
                    else
                    {
                        prop.Value.TraverseForNullValueValidation(errors, nullNodes, prop.Name);
                    }
                }
            }
        }

        private static void TraverseForUnknownFieldType(this JToken token, List<Error> errors, Type type, string path = null)
        {
            // if type contains JsonExtensionDataAttribute, additional properties are allowed
            if (CheckTypeContainsJsonExtensionData(type))
                return;

            path = BuildPath(path, type);
            if (token is JArray array)
            {
                foreach (var item in token.Children())
                {
                    item.TraverseForUnknownFieldType(errors, type, path);
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

                    var nestedType = CheckForUnknownField(type, prop, errors, path);
                    prop.Value.TraverseForUnknownFieldType(errors, nestedType, path);
                }
            }
        }

        private static string BuildPath(string path, Type type)
        {
            return path is null ? type.Name : $"{path}.{type.Name}";
        }

        private static bool CheckTypeContainsJsonExtensionData(Type type)
        {
            return s_cacheTypeContainsJsonExtensionData.GetOrAdd(
                type,
                new Lazy<bool>(() => type.GetProperties().Any(prop => prop.GetCustomAttribute<JsonExtensionDataAttribute>() != null))).Value;
        }

        private static Type CheckForUnknownField(Type type, JProperty prop, List<Error> errors, string path)
        {
            var contract = DefaultDeserializer.ContractResolver.ResolveContract(type);
            JsonPropertyCollection jsonProperties;
            if (contract is JsonObjectContract objectContract)
            {
                jsonProperties = objectContract.Properties;
            }
            else if (contract is JsonArrayContract arrayContract)
            {
                jsonProperties = GetPropertiesFromJsonArrayContract(arrayContract);
            }
            else
            {
                return type;
            }

            // if mismatching field found, add error
            // else, pass along with nested type
            var matchingProperty = jsonProperties.GetClosestMatchProperty(prop.Name);
            if (matchingProperty is null)
            {
                var lineInfo = prop as IJsonLineInfo;
                errors.Add(Errors.UnknownField(
                    new Range(lineInfo.LineNumber, lineInfo.LinePosition), prop.Name, type.Name, $"{path}.{prop.Name}"));
                return type;
            }
            else
            {
                return matchingProperty.PropertyType;
            }
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

        private static void LogInfoForNullValue(IJsonLineInfo token, List<Error> errors, string name)
        {
            errors.Add(Errors.NullValue(new Range(token.LineNumber, token.LinePosition), name));
        }

        private sealed class JsonContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                var converter = GetConverter(member);

                if (!prop.Writable)
                {
                    if (member is FieldInfo f && f.IsPublic && !f.IsStatic)
                    {
                        prop.Writable = true;
                    }
                }

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
                return prop;
            }

            private static SchemaValidationConverter GetConverter(MemberInfo member)
            {
                var validators = member.GetCustomAttributes<ValidationAttribute>(false).ToList();
                return validators.Count == 0 ? null : new SchemaValidationConverter(validators);
            }
        }

        private sealed class SchemaValidationConverter : JsonConverter
        {
            private readonly IEnumerable<ValidationAttribute> _validators;

            public SchemaValidationConverter(IEnumerable<ValidationAttribute> validators)
            {
                _validators = validators;
            }

            public override bool CanConvert(Type objectType) => true;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        var obj = JObject.Load(reader);
                        Validate(reader, obj);
                        break;
                    case JsonToken.StartArray:
                        var array = JArray.Load(reader);
                        Validate(reader, array);
                        break;
                    case JsonToken.StartConstructor:
                        var constructor = JConstructor.Load(reader);
                        Validate(reader, constructor);
                        break;
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.String:
                    case JsonToken.Boolean:
                    case JsonToken.Date:
                    case JsonToken.Bytes:
                    case JsonToken.Raw:
                    case JsonToken.None:
                    case JsonToken.Null:
                    case JsonToken.Undefined:
                        Validate(reader, reader.Value);
                        break;
                    case JsonToken.PropertyName:
                    case JsonToken.Comment:
                    case JsonToken.EndObject:
                    case JsonToken.EndArray:
                    case JsonToken.EndConstructor:
                    default:
                        break;
                }
                return reader.Value;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            private void Validate(JsonReader reader, object value)
            {
                foreach (var validator in _validators)
                {
                    if (validator != null && !validator.IsValid(value))
                    {
                        var lineInfo = reader as IJsonLineInfo;
                        var range = new Range(lineInfo.LineNumber, lineInfo.LinePosition);
                        var validationResult = validator.GetValidationResult(value, new ValidationContext(value, null));
                        throw Errors.ViolateSchema(range, validationResult.ErrorMessage).ToException();
                    }
                }
            }
        }
    }
}
