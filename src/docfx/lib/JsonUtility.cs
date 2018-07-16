// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        /// Deserialize from TextReader to an object
        /// </summary>
        public static (List<Error>, T) Deserialize<T>(TextReader reader)
        {
            var (errors, token) = Deserialize(reader);
            return (errors, token.ToObject<T>(DefaultDeserializer));
        }

        /// <summary>
        /// Deserialize a string to an object
        /// </summary>
        public static (List<Error>, T) Deserialize<T>(string json)
        {
            return Deserialize<T>(new StringReader(json));
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

        public static (List<Error>, JToken) ValidateNullValue(this JToken token, IDictionary<string, Range> mappings = null)
        {
            var errors = new List<Error>();
            var nullNodes = new List<JToken>();
            token.Traverse(errors, mappings, nullNodes);
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

        /// <summary>
        /// Parse a string to JToken.
        /// Validate null value during the process.
        /// </summary>
        private static (List<Error>, JToken) Deserialize(TextReader reader)
        {
            try
            {
                using (JsonReader json = new JsonTextReader(reader))
                {
                    var (errors, token) = DefaultDeserializer.Deserialize<JToken>(json).ValidateNullValue();
                    return (errors, token ?? JValue.CreateNull());
                }
            }
            catch (Exception ex)
            {
                throw Errors.JsonSyntaxError(ex).ToException();
            }
        }

        private static void Traverse(this JToken token, List<Error> errors, IDictionary<string, Range> mappings, List<JToken> nullNodes, string name = null)
        {
            if (token is JArray array)
            {
                // Adding index to path to have unique path for array items
                var index = 0;
                foreach (var item in token.Children())
                {
                    if (item.IsNullOrUndefined())
                    {
                        LogInfoForNullValue(array, errors, mappings, name);
                        nullNodes.Add(item);
                    }
                    else
                    {
                        Traverse(item, errors, mappings, nullNodes, string.Join('|', name, index++));
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
                        LogInfoForNullValue(token, errors, mappings, string.Join('|', name, prop.Name));
                        nullNodes.Add(item);
                    }
                    else
                    {
                        prop.Value.Traverse(errors, mappings, nullNodes, string.Join('|', name, prop.Name));
                    }
                }
            }
        }

        private static void LogInfoForNullValue(JToken item, List<Error> errors, IDictionary<string, Range> mappings, string name)
        {
            if (mappings == null)
            {
                var lineInfo = item as IJsonLineInfo;
                errors.Add(Errors.NullValue(new Range(lineInfo.LineNumber, lineInfo.LinePosition), name.Split('|').Last()));
            }
            else
            {
                Debug.Assert(mappings.ContainsKey(name));
                var value = mappings[name];
                errors.Add(Errors.NullValue(new Range(value.StartLine, value.StartCharacter, value.EndLine, value.EndCharacter), name.Split('|').Last()));
            }
        }

        private sealed class JsonContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);

                if (!prop.Writable)
                {
                    if (member is FieldInfo f && f.IsPublic && !f.IsStatic)
                    {
                        prop.Writable = true;
                    }
                }
                return prop;
            }
        }
    }
}
