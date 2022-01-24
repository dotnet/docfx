// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class JTokenDeepEqualsComparer : IEqualityComparer<JToken>
{
    public bool Equals(JToken? a, JToken? b)
    {
        switch (a)
        {
            case null:
                return b is null;

            case JValue valueA when b is JValue valueB:
                return Equals(TryConvertDoubleToLong(valueA.Value), TryConvertDoubleToLong(valueB.Value));

            case JArray arrayA when b is JArray arrayB:
                if (arrayA.Count != arrayB.Count)
                {
                    return false;
                }

                // Array property order MATTERS
                for (var i = 0; i < arrayA.Count; i++)
                {
                    if (!Equals(arrayA[i], arrayB[i]))
                    {
                        return false;
                    }
                }
                return true;

            case JObject mapA when b is JObject mapB:
                if (mapA.Count != mapB.Count)
                {
                    return false;
                }

                // Object property order DOES NOT MATTER
                foreach (var (key, valueA) in mapA)
                {
                    if (!mapB.TryGetValue(key, out var valueB) || !Equals(valueA, valueB))
                    {
                        return false;
                    }
                }
                return true;

            default:
                return false;
        }
    }

    public int GetHashCode(JToken token) => token switch
    {
        JValue value when value.Value != null => value.Value.GetHashCode(),
        JArray array => array.Count,
        JObject obj => obj.Count,
        _ => 0,
    };

    private static object? TryConvertDoubleToLong(object? value)
    {
        if (value is double d && (long)d == d)
        {
            return (long)d;
        }
        return value;
    }
}
