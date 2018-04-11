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

        private static readonly JsonSerializer s_defaultNoneFormatSerializer = JsonSerializer.Create(s_noneFormatJsonSerializerSettings);
        private static readonly JsonSerializer s_defaultIndentedFormatSerializer = JsonSerializer.Create(s_indentedFormatJsonSerializerSettings);

        /// <summary>
        /// Serialize an object to TextWriter
        /// </summary>
        public static void Serialize(TextWriter writer, object graph, bool isStable = false, Formatting formatting = Formatting.None)
        {
            var localSerializer = isStable || formatting == Formatting.Indented ? s_defaultIndentedFormatSerializer : s_defaultNoneFormatSerializer;
            if (isStable)
            {
                localSerializer.Serialize(writer, Stablize(JToken.FromObject(graph, localSerializer)));
            }
            else
            {
                localSerializer.Serialize(writer, graph);
            }
        }

        /// <summary>
        /// Serialize an object to string
        /// </summary>
        public static string Serialize(object graph, bool isStable = false, Formatting formatting = Formatting.None)
        {
            using (StringWriter writer = new StringWriter())
            {
                Serialize(writer, graph, isStable, formatting);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Deserialize from TextReader to an object
        /// </summary>
        public static T Deserialize<T>(TextReader reader)
        {
            using (JsonReader json = new JsonTextReader(reader))
            {
                return s_defaultNoneFormatSerializer.Deserialize<T>(json);
            }
        }

        /// <summary>
        /// Deserialize a string to an object
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return Deserialize<T>(new StringReader(json));
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
