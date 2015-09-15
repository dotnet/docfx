// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// The utility class for docascode project
/// </summary>
namespace Microsoft.DocAsCode.EntityModel
{
    using System;
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

        public static void Serialize(string path, object graph, Formatting formatting = Formatting.None)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter writer = new StreamWriter(path))
            {
                serializer.Value.Formatting = formatting;
                serializer.Value.Serialize(writer, graph);
            }
        }

        public static T Deserialize<T>(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                using (JsonReader json = new JsonTextReader(reader))
                {
                    return serializer.Value.Deserialize<T>(json);
                }
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
            return deserializer.Value.Deserialize<T>(reader);
        }

        public static T Deserialize<T>(string path)
        {
            using (StreamReader reader = new StreamReader(path))
                return Deserialize<T>(reader);
        }
    }
}
