// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;

    public static class CollectionUtility
    {
        public static Dictionary<string, List<T>> Merge<T>(this IDictionary<string, List<T>> left, IEnumerable<KeyValuePair<string, IEnumerable<T>>> right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            var result = new Dictionary<string, List<T>>(left);
            if (right == null)
            {
                return result;
            }
            foreach (var pair in right)
            {
                if (result.TryGetValue(pair.Key, out List<T> list))
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
