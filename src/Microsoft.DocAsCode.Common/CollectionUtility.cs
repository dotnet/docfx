// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

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

        public static void Merge<T>(this Dictionary<string, List<T>> left, IEnumerable<KeyValuePair<string, ImmutableList<T>>> right)
        {
            if (right != null && left != null)
            {
                foreach (var pair in right)
                {
                    if (left.TryGetValue(pair.Key, out List<T> list))
                    {
                        list.AddRange(pair.Value);
                    }
                    else
                    {
                        left[pair.Key] = new List<T>(pair.Value);
                    }
                }
            }
        }

        public static ImmutableDictionary<string, ImmutableList<T>> Merge<T, TRight>(this ImmutableDictionary<string, ImmutableList<T>> left, IEnumerable<KeyValuePair<string, TRight>> right)
            where TRight: IEnumerable<T>
        {
            if (right == null)
            {
                return left;
            }

            return left.ToDictionary(s => s.Key, s => s.Value.ToList())
               .Merge(right.Select(s => new KeyValuePair<string, IEnumerable<T>>(s.Key, s.Value)))
               .ToImmutableDictionary(s => s.Key, s => s.Value.ToImmutableList());
        }
    }
}
