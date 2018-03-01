// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.IO;
    using System.Threading;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using Microsoft.DocAsCode.Plugins;

    public static class JsonUtility
    {
        public static readonly ThreadLocal<JsonSerializer> DefaultSerializer = new ThreadLocal<JsonSerializer>(
            () => new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Converters =
                {
                    new StringEnumConverter { CamelCaseText = true },
                }
            });

        public static void Serialize(TextWriter writer, object graph, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
        {
            var localSerializer = serializer ?? DefaultSerializer.Value;
            localSerializer.Formatting = formatting;
            localSerializer.Serialize(writer, graph);
        }

        public static string Serialize(object graph, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
        {
            using (StringWriter writer = new StringWriter())
            {
                Serialize(writer, graph, formatting, serializer);
                return writer.ToString();
            }
        }

        public static void Serialize(string path, object graph, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
        {
            using (var stream = EnvironmentContext.FileAbstractLayer.Create(path))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                Serialize(writer, graph, formatting, serializer);
            }
        }

        public static T Deserialize<T>(string path, JsonSerializer serializer = null)
        {
            using (var stream = EnvironmentContext.FileAbstractLayer.OpenRead(path))
            using (StreamReader reader = new StreamReader(stream))
            {
                return Deserialize<T>(reader, serializer);
            }
        }

        public static T Deserialize<T>(TextReader reader, JsonSerializer serializer = null)
        {
            using (JsonReader json = new JsonTextReader(reader))
            {
                return (serializer ?? DefaultSerializer.Value).Deserialize<T>(json);
            }
        }

        public static string ToJsonString(this object graph, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
        {
            var sw = new StringWriter();
            Serialize(sw, graph, formatting, serializer);
            return sw.ToString();
        }

        public static T FromJsonString<T>(this string json, JsonSerializer serializer = null)
        {
            return Deserialize<T>(new StringReader(json), serializer);
        }
    }
}
