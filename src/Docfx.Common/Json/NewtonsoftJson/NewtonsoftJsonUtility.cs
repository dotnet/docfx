// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Docfx.Common;

internal static class NewtonsoftJsonUtility
{
    public static readonly ThreadLocal<JsonSerializer> DefaultSerializer = new(
        () => new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            Converters =
            {
                new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() },
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
        using StringWriter writer = new();
        Serialize(writer, graph, formatting, serializer);
        return writer.ToString();
    }

    public static void Serialize(string path, object graph, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
    {
        using var stream = EnvironmentContext.FileAbstractLayer.Create(path);
        using StreamWriter writer = new(stream);
        Serialize(writer, graph, formatting, serializer);
    }

    public static T Deserialize<T>(string path, JsonSerializer serializer = null)
    {
        using var stream = EnvironmentContext.FileAbstractLayer.OpenRead(path);
        using StreamReader reader = new(stream);
        return Deserialize<T>(reader, serializer);
    }

    public static T Deserialize<T>(TextReader reader, JsonSerializer serializer = null)
    {
        using JsonReader json = new JsonTextReader(reader);
        return (serializer ?? DefaultSerializer.Value).Deserialize<T>(json);
    }

    public static string ToJsonString(object graph, Formatting formatting = Formatting.None, JsonSerializer serializer = null)
    {
        var sw = new StringWriter();
        Serialize(sw, graph, formatting, serializer);
        return sw.ToString();
    }

    public static T FromJsonString<T>(string json, JsonSerializer serializer = null)
    {
        return Deserialize<T>(new StringReader(json), serializer);
    }
}
