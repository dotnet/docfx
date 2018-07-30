// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;

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
            return (errors, (T)token.ToObject(typeof(T), DefaultDeserializer));
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

        public static (List<Error>, JToken) ValidateNullValue(this JToken token)
        {
            var errors = new List<Error>();
            var nullNodes = new List<JToken>();
            token.Traverse(errors, nullNodes);
            foreach (var node in nullNodes)
            {
                node.Remove();
            }
            return (errors, token);
        }

        private static bool IsNullOrUndefined(this JToken token)
        {
            return
                (token == null) ||
                (token.Type == JTokenType.Null) ||
                (token.Type == JTokenType.Undefined);
        }

        private static void Traverse(this JToken token, List<Error> errors, List<JToken> nullNodes, string name = null)
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
                        Traverse(item, errors, nullNodes, name);
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
                        prop.Value.Traverse(errors, nullNodes, prop.Name);
                    }
                }
            }
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
                var validator = member.GetCustomAttribute<ValidationAttribute>();
                return validator is null ? null : new SchemaValidationConverter(validator);
            }
        }

        private sealed class SchemaValidationConverter : JsonConverter
        {
            private readonly ValidationAttribute _validator;

            public SchemaValidationConverter(ValidationAttribute validator)
            {
                _validator = validator;
            }

            public override bool CanConvert(Type objectType) => true;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (_validator != null && !_validator.IsValid(reader.Value))
                {
                    var lineInfo = reader as IJsonLineInfo;
                    var range = new Range(lineInfo.LineNumber, lineInfo.LinePosition);
                    var validationResult = _validator.GetValidationResult(reader.Value, new ValidationContext(reader.Value, null));
                    throw Errors.InValidSchema(range, validationResult.ErrorMessage).ToException();
                }
                return reader.Value;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
