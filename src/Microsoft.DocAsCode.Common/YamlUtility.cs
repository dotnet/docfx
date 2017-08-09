// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.IO;
    using System.Text;
    using System.Threading;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.YamlSerialization;

    using YamlDotNet.Serialization;

    using YamlDeserializer = Microsoft.DocAsCode.YamlSerialization.YamlDeserializer;

    public static class YamlUtility
    {
        private static readonly ThreadLocal<YamlSerializer> serializer = new ThreadLocal<YamlSerializer>(() => new YamlSerializer(SerializationOptions.DisableAliases));
        private static readonly ThreadLocal<YamlDeserializer> deserializer = new ThreadLocal<YamlDeserializer>(() => new YamlDeserializer(ignoreUnmatched: true));

        public static void Serialize(TextWriter writer, object graph)
        {
            Serialize(writer, graph, null);
        }

        public static void Serialize(TextWriter writer, object graph, string comments)
        {
            if (!string.IsNullOrEmpty(comments))
            {
                foreach (var comment in comments.Split('\n'))
                {
                    writer.Write("### ");
                    writer.WriteLine(comment.TrimEnd('\r'));
                }
            }
            serializer.Value.Serialize(writer, graph);
        }

        public static void Serialize(string path, object graph)
        {
            Serialize(path, graph, null);
        }

        public static void Serialize(string path, object graph, string comments)
        {
            using (var writer = EnvironmentContext.FileAbstractLayer.CreateText(path))
            {
                Serialize(writer, graph, comments);
            }
        }

        public static T Deserialize<T>(TextReader reader)
        {
            return deserializer.Value.Deserialize<T>(reader);
        }

        public static T Deserialize<T>(string path)
        {
            using (var reader = EnvironmentContext.FileAbstractLayer.OpenReadText(path))
            {
                return Deserialize<T>(reader);
            }
        }

        public static T ConvertTo<T>(object obj)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                serializer.Value.Serialize(writer, obj);
            }
            return deserializer.Value.Deserialize<T>(new StringReader(sb.ToString()));
        }
    }
}
