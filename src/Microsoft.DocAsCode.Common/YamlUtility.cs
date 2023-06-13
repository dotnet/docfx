// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

using Microsoft.DocAsCode.Plugins;
using Microsoft.DocAsCode.YamlSerialization;

using YamlDeserializer = Microsoft.DocAsCode.YamlSerialization.YamlDeserializer;

namespace Microsoft.DocAsCode.Common;

public static class YamlUtility
{
    private static readonly ThreadLocal<YamlSerializer> serializer = new(() => new YamlSerializer(SerializationOptions.DisableAliases));
    private static readonly ThreadLocal<YamlDeserializer> deserializer = new(() => new YamlDeserializer(ignoreUnmatched: true));

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
        using var writer = EnvironmentContext.FileAbstractLayer.CreateText(path);
        Serialize(writer, graph, comments);
    }

    public static T Deserialize<T>(TextReader reader)
    {
        return deserializer.Value.Deserialize<T>(reader);
    }

    public static T Deserialize<T>(string path)
    {
        using var reader = EnvironmentContext.FileAbstractLayer.OpenReadText(path);
        return Deserialize<T>(reader);
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
