// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class CollectionUtility
    {
        public static void AddIfNotNull<T>(this IList<T> list, T? value) where T : class
        {
            if (!(value is null))
            {
                list.Add(value);
            }
        }

        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> range) where TKey : notnull
        {
            foreach (var (key, value) in range)
            {
                dictionary.Add(key, value);
            }
        }
    }
}
