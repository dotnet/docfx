// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx.Common;

public static class JsonUtility
{
    public static string Serialize<T>(T graph, bool indented = false)
    {
        if (IsSystemTextJsonSupported(graph))
            return SystemTextJsonUtility.Serialize(graph, indented);
        else
            return NewtonsoftJsonUtility.Serialize(graph, indented ? Formatting.Indented : Formatting.None);
    }

    public static void Serialize<T>(string path, T graph, bool indented = false)
    {
        if (IsSystemTextJsonSupported(graph))
            SystemTextJsonUtility.SerializeToFile(path, graph, indented);
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

    internal static bool IsSystemTextJsonSupported<T>()
    {
        return StaticTypeCache<T>.Supported;
    }

    internal static bool IsSystemTextJsonSupported<T>(T obj)
    {
        if (typeof(T) == typeof(object) && obj.GetType().FullName.StartsWith("Newtonsoft.", StringComparison.Ordinal))
            return false;

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
            var fullName = typeof(T).FullName;
            switch (fullName)
            {
                // Use Newtonsoft.Json for RestAPI models.
                case "Docfx.DataContracts.RestApi.RestApiRootItemViewModel":

                // Some unit tests using Newtonsoft.Json types.
                case "Newtonsoft.Json.Linq.JObject":
                    return false;

                default:
                    return true;
            }
        }
    }
}
