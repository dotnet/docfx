using System.IO;
using System.Text;
using System.Threading;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.YamlHelpers
{
    public static class YamlUtility
    {
        private static readonly ThreadLocal<ISerializer> serializer
            = new ThreadLocal<ISerializer>(() =>
            new SerializerBuilder()
            .DisableAliases()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .WithEventEmitter(next => new StringQuotingEmitter(next))
            .Build());
        private static readonly ThreadLocal<IDeserializer> deserializer
            = new ThreadLocal<IDeserializer>(() => new DeserializerBuilder().IgnoreUnmatchedProperties().Build());

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
            using (var writer = new StreamWriter(path))
            {
                Serialize(writer, graph, comments);
            }
        }

        public static T Deserialize<T>(TextReader reader)
        {
            return deserializer.Value.Deserialize<T>(reader);
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
