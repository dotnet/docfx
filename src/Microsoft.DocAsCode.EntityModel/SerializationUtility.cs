// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// The utility class for docascode project
/// </summary>
namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public static class JsonUtility
    {
        private static readonly ThreadLocal<JsonSerializer> serializer = new ThreadLocal<JsonSerializer>(
            () =>
                {
                    var jsonSerializer = new JsonSerializer();
                    jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                    jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                    jsonSerializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter { CamelCaseText = true });
                    return jsonSerializer;
                });

        public static void Serialize(TextWriter writer, object graph, Formatting formatting = Formatting.None)
        {
            serializer.Value.Formatting = formatting;
            serializer.Value.Serialize(writer, graph);
        }

        public static string Serialize(object graph, Formatting formatting = Formatting.None)
        {
            using (StringWriter writer = new StringWriter())
            {
                Serialize(writer, graph, formatting);
                return writer.ToString();
            }
        }

        public static void Serialize(string path, object graph, Formatting formatting = Formatting.None)
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

        public static T Deserialize<T>(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                return Deserialize<T>(reader);
            }
        }

        public static T Deserialize<T>(TextReader reader)
        {
            using (JsonReader json = new JsonTextReader(reader))
            {
                return serializer.Value.Deserialize<T>(json);
            }
        }
    }

    public static class YamlUtility
    {
        private static readonly ThreadLocal<Serializer> serializer = new ThreadLocal<Serializer>(() => new Serializer());
        private static readonly ThreadLocal<Deserializer> deserializer = new ThreadLocal<Deserializer>(() => new Deserializer(ignoreUnmatched:true));

        public static void Serialize(TextWriter writer, object graph)
        {
            serializer.Value.Serialize(writer, graph);
        }

        public static void Serialize(string path, object graph)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter writer = new StreamWriter(path))
            {
                Serialize(writer, graph);
            }
        }

        public static T Deserialize<T>(TextReader reader)
        {
            // For weak type, directly deserialize using yamldotnet
            if (typeof(T) == typeof(Dictionary<string, object>))
            {
                return deserializer.Value.Deserialize<T>(reader);
            }

            // YamlDotNet is slow in deserialize into strong typed model.
            // Use JSON.NET to convert from dictionary to object model instead.
            var dict = deserializer.Value.Deserialize(reader);
            var json = JsonUtility.Serialize(dict);
            using (var stringReader = new StringReader(json))
            {
                return JsonUtility.Deserialize<T>(stringReader);
            }
        }

        public static T Deserialize<T>(string path)
        {
            using (StreamReader reader = new StreamReader(path))
                return Deserialize<T>(reader);
        }
    }
}
