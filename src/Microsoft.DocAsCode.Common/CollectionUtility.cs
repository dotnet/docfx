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

        public static ImmutableDictionary<string, ImmutableList<T>> Merge<T>(this ImmutableDictionary<string, ImmutableList<T>> left, Dictionary<string, List<T>> right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right == null)
            {
                return left;
            }

            return left.Select(i => new KeyValuePair<string, IEnumerable<T>>(i.Key, i.Value)).Merge(
                right.Select(i => new KeyValuePair<string, IEnumerable<T>>(i.Key, i.Value)))
                .ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList());
        }

        public static IEnumerable<KeyValuePair<string, IEnumerable<T>>> Merge<T>(this IEnumerable<KeyValuePair<string, IEnumerable<T>>> left, IEnumerable<KeyValuePair<string, IEnumerable<T>>> right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right == null)
            {
                return left;
            }

            return left.Concat(right)
                .GroupBy(p => p.Key, p => p.Value)
                .Select(p => new KeyValuePair<string, IEnumerable<T>>(p.Key, p.SelectMany(i => i)));
        }
    }
}
