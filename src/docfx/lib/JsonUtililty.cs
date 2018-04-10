// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs
{
    /// <summary>
    /// Provide Utilities of Json
    /// </summary>
    public static class JsonUtililty
    {
        private static readonly JsonSerializer s_defaultSerializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Converters =
                {
                    new StringEnumConverter { CamelCaseText = true },
                },
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };

        private static readonly JsonMergeSettings s_jsonMergeSettings = new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
        };

        /// <summary>
        /// Serialize an object to TextWriter
        /// </summary>
        public static void Serialize(TextWriter writer, object graph, bool isStable = false, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
        {
            var localSerializer = serializer ?? s_defaultSerializer;
            if (isStable)
            {
                localSerializer.Formatting = Formatting.Indented;
                try
                {
                    localSerializer.Serialize(writer, Stablize(JToken.FromObject(graph, localSerializer)));
                }
                finally
                {
                    localSerializer.Formatting = Formatting.None;
                }
            }
            else
            {
                localSerializer.Formatting = formatting;
                localSerializer.Serialize(writer, graph);
            }
        }

        /// <summary>
        /// Serialize an object to string
        /// </summary>
        public static string Serialize(object graph, bool isStable = false, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
        {
            using (StringWriter writer = new StringWriter())
            {
                Serialize(writer, graph, isStable, formatting, serializer);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Deserialize from TextReader to an object
        /// </summary>
        public static T Deserialize<T>(TextReader reader, JsonSerializer serializer = null)
        {
            using (JsonReader json = new JsonTextReader(reader))
            {
                return (serializer ?? s_defaultSerializer).Deserialize<T>(json);
            }
        }

        /// <summary>
        /// Deserialize a string to an object
        /// </summary>
        public static T Deserialize<T>(string json, JsonSerializer serializer = null)
        {
            return Deserialize<T>(new StringReader(json), serializer);
        }

        private static JToken Stablize(JToken token)
        {
            if (token is JObject obj)
            {
                return new JObject(obj.Properties()
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .Select(p => new JProperty(p.Name, Stablize(p.Value))));
            }

            if (token is JArray arr)
            {
                return new JArray(arr.Select(i => Stablize(i)));
            }

            return token;
        }
    }
}
