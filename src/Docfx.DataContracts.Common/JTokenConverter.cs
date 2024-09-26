// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Docfx.Common;
using Newtonsoft.Json.Linq;

namespace Docfx.DataContracts.Common;

public static class JTokenConverter
{
    public static T Convert<T>(object obj)
    {
        if (obj is T tObj)
        {
            return tObj;
        }

        if (obj is JToken jToken)
        {
            return jToken.ToObject<T>();
        }

        // Custom code path for `System.Text.Json` deserialization.
        // `ObjectToInferredTypesConverter` deserialize items as `List<object>`.
        // So it need to convert `List<object>` to `List<TItem>`.
        if (obj is List<object> list && IsListType<T>())
        {
            var json = JsonUtility.Serialize(list);
            var result = JsonUtility.Deserialize<T>(new StringReader(json));
            return result;
        }

        throw new InvalidCastException();
    }

    private static bool IsListType<T>()
    {
        var type = typeof(T);
        return type.GetTypeInfo().IsGenericType
            && type.GetGenericTypeDefinition() == typeof(List<>);
    }
}
