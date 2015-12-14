// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.IO;
    using System.Threading;

    using Microsoft.DocAsCode.YamlSerializations;

    public static class YamlUtility
    {
        private static readonly ThreadLocal<YamlSerializer> serializer = new ThreadLocal<YamlSerializer>(() => new YamlSerializer());
        private static readonly ThreadLocal<YamlDeserializer> deserializer = new ThreadLocal<YamlDeserializer>(() => new YamlDeserializer(ignoreUnmatched: true));

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
