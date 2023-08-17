// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace Docfx.Common;

public static class ConvertToObjectHelper
{
#nullable enable
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static object? ConvertJObjectToObject(object raw)
    {
        switch (raw)
        {
            case null:
                return null;
            case JToken jToken:
                return ConvertJTokenToObject(jToken);
            default:
                return raw; //  if other type object passed. Return object itself.
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static object? ConvertJTokenToObject([NotNull] JToken jToken)
    {
        switch (jToken.Type)
        {
            case JTokenType.Array:
                return ConvertJArrayToObjectArray((JArray)jToken);
            case JTokenType.Object:
                return ConvertJObjectToDictionary((JObject)jToken);
            default:
                if (jToken is JValue jValue)
                    return jValue.Value;
                else
                    throw new ArgumentException($"Not expected object type passed. JTokenType: {jToken.Type}, JToken: {jToken}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static object?[] ConvertJArrayToObjectArray([NotNull] JArray jArray)
    {
        return jArray.Select(ConvertJTokenToObject).ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static IDictionary<string, object?> ConvertJObjectToDictionary(JObject jObject)
    {
        var dictionary = (IDictionary<string, JToken>)jObject;
        return dictionary.ToDictionary(p => p.Key,
                                       p => p.Value == null
                                         ? null
                                         : ConvertJTokenToObject(p.Value));
    }
#nullable restore

    public static object ConvertStrongTypeToObject(object raw)
    {
        return ConvertJObjectToObject(ConvertStrongTypeToJObject(raw));
    }

    public static object ConvertStrongTypeToJObject(object raw)
    {
        if (raw is JToken)
        {
            return raw;
        }

        return JToken.FromObject(raw, JsonUtility.DefaultSerializer.Value);
    }

    public static object ConvertExpandoObjectToObject(object raw)
    {
        return ConvertExpandoObjectToObjectCore(raw, new Dictionary<object, object>());
    }

    public static object ConvertToDynamic(object obj)
    {
        return ConvertToDynamicCore(obj, new Dictionary<object, object>());
    }

    private static object ConvertExpandoObjectToObjectCore(object obj, Dictionary<object, object> cache)
    {
        if (obj == null)
        {
            return null;
        }

        if (cache.TryGetValue(obj, out var output))
        {
            return output;
        }

        var result = obj;

        if (obj is ExpandoObject eo)
        {
            result = cache[obj] = new Dictionary<string, object>();
            foreach (var pair in eo)
            {
                ((Dictionary<string, object>)result)[pair.Key] = ConvertExpandoObjectToObjectCore(pair.Value, cache);
            }
        }
        else if (obj is IEnumerable<object> enumerable)
        {
            result = cache[obj] = new List<object>();
            foreach (var item in enumerable)
            {
                ((List<object>)result).Add(ConvertExpandoObjectToObjectCore(item, cache));
            }
        }

        return result;
    }

    private static object ConvertToDynamicCore(object obj, Dictionary<object, object> cache)
    {
        if (obj == null)
        {
            return null;
        }

        if (cache.TryGetValue(obj, out var output))
        {
            return output;
        }
        var result = obj;
        if (obj is ExpandoObject)
        {
            result = cache[obj] = obj;
        }
        else if (obj is IDictionary<object, object> dict)
        {
            result = cache[obj] = new ExpandoObject();

            foreach (var pair in dict)
            {
                if (!(pair.Key is string key))
                {
                    throw new NotSupportedException("Only string key is supported.");
                }

                ((IDictionary<string, Object>)result).Add(key, ConvertToDynamicCore(pair.Value, cache));
            }
        }
        else if (obj is IDictionary<string, object> sDict)
        {
            result = cache[obj] = new ExpandoObject();

            foreach (var pair in sDict)
            {
                ((IDictionary<string, Object>)result).Add(pair.Key, ConvertToDynamicCore(pair.Value, cache));
            }
        }
        else if (obj is IList<object> array)
        {
            result = cache[obj] = array;
            for (int i = 0; i < array.Count; i++)
            {
                ((IList<object>)result)[i] = ConvertToDynamicCore(array[i], cache);
            }
        }

        return result;
    }
}
