// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DocAsCode.Common
{
    public static class CollectionUtility
    {
        public static Dictionary<string, List<T>> Merge<T>(this IDictionary<string, List<T>> left, IEnumerable<KeyValuePair<string, IEnumerable<T>>> right)
        {
            var result = new Dictionary<string, List<T>>(left);
            foreach (var pair in right)
            {
                List<T> list;
                if (result.TryGetValue(pair.Key, out list))
                {
                    list.AddRange(pair.Value);
                }
                else
                {
                    result[pair.Key] = new List<T>(pair.Value);
                }
            }

            return result;
        }
    }
}
