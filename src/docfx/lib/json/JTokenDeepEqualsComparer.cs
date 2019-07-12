// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JTokenDeepEqualsComparer : IEqualityComparer<JToken>
    {
        public bool Equals(JToken a, JToken b)
        {
            switch (a)
            {
                case null:
                    return b is null;

                case JValue valueA when b is JValue valueB:
                    return Equals(valueA.Value, valueB.Value);

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

        public int GetHashCode(JToken token)
        {
            switch (token)
            {
                case JValue value when value.Value != null:
                    return value.Value.GetHashCode();

                case JArray array:
                    return array.Count;

                case JObject obj:
                    return obj.Count;

                default:
                    return 0;
            }
        }
    }
}
