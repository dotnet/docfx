// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Docfx.Plugins;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Common;

public static class JsonUtility
{
    public static string Serialize<T>(T graph, bool indented = false)
    {
        if (IsSystemTextJsonSupported<T>())
            return SystemTextJsonUtility.Serialize<T>(graph, indented);
        else
            return NewtonsoftJsonUtility.Serialize(graph, indented ? Formatting.Indented : Formatting.None);
    }

    public static void Serialize<T>(string path, T graph, bool indented = false)
    {
        if (IsSystemTextJsonSupported<T>())
            SystemTextJsonUtility.SerializeToFile<T>(path, graph, indented);
        else
            NewtonsoftJsonUtility.Serialize(path, graph, indented ? Formatting.Indented : Formatting.None);
    }

    public static T Deserialize<T>(string path)
    {
        if (IsSystemTextJsonSupported<T>())
            return SystemTextJsonUtility.DeserializeFromFile<T>(path);
        else
            return NewtonsoftJsonUtility.Deserialize<T>(path);
    }

    public static T Deserialize<T>(TextReader reader)
    {
        if (IsSystemTextJsonSupported<T>())
            return SystemTextJsonUtility.Deserialize<T>(reader.ReadToEnd());
        else
            return NewtonsoftJsonUtility.Deserialize<T>(reader);
    }

    internal static ValueTask<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
        if (IsSystemTextJsonSupported<T>())
            return SystemTextJsonUtility.DeserializeAsync<T>(stream, cancellationToken);
        else
            throw new NotSupportedException();
    }

    public static string ToJsonString<T>(this T graph)
    {
        if (IsSystemTextJsonSupported<T>())
            return SystemTextJsonUtility.Serialize<T>(graph);
        else
            return NewtonsoftJsonUtility.ToJsonString(graph);
    }

    public static T FromJsonString<T>(this string json)
    {
        if (IsSystemTextJsonSupported<T>())
            return SystemTextJsonUtility.Deserialize<T>(json);
        else
            return NewtonsoftJsonUtility.FromJsonString<T>(json);
    }

    private static bool IsSystemTextJsonSupported<T>()
    {
        return StaticTypeCache<T>.Supported;
    }

    private static class StaticTypeCache<T>
    {
        public static readonly bool Supported;

        static StaticTypeCache()
        {
            Supported = IsSupported();
        }

        private static bool IsSupported()
        {
            var type = typeof(T);
            var fullName = type.FullName;

            // TODO: Return `true` for types that support serialize/deserializenon with System.Text.Json.
            switch (fullName)
            {
                case "Docfx.Build.Engine.XRefMap":
                    return true;
                
                default:
                    return false;
            }
        }
    }
}
