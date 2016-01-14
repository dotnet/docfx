// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.IO;
    using System.Threading;

    using Newtonsoft.Json;

    public static class JsonUtility
    {
        private static readonly ThreadLocal<JsonSerializer> _serializer = new ThreadLocal<JsonSerializer>(
            () =>
                {
                    var jsonSerializer = new JsonSerializer();
                    jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                    jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                    jsonSerializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter { CamelCaseText = true });
                    return jsonSerializer;
                });

        public static void Serialize(TextWriter writer, object graph, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
        {
            var localSerializer = serializer ?? _serializer.Value;
            localSerializer.Formatting = formatting;
            localSerializer.Serialize(writer, graph);
        }

        public static string Serialize(object graph, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
        {
            using (StringWriter writer = new StringWriter())
            {
                Serialize(writer, graph, formatting);
                return writer.ToString();
            }
        }

        public static void Serialize(string path, object graph, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter writer = new StreamWriter(path))
            {
                Serialize(writer, graph, formatting);
            }
        }

        public static T Deserialize<T>(string path, JsonSerializer serializer = null)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                return Deserialize<T>(reader, serializer);
            }
        }

        public static T Deserialize<T>(TextReader reader, JsonSerializer serializer = null)
        {
            using (JsonReader json = new JsonTextReader(reader))
            {
                return (serializer ?? _serializer.Value).Deserialize<T>(json);
            }

        }
    }
}
