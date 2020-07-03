// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Docs.Build;

namespace System.Collections.Concurrent
{
    internal class MemoryCache<TKey, TValue> : ConcurrentDictionary<TKey, TValue> where TKey : notnull
    {
        public MemoryCache()
            : base()
        {
            MemoryMonitor.AddMemoryMonitor(Trim);
        }

        public MemoryCache(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
            MemoryMonitor.AddMemoryMonitor(Trim);
        }

        private void Trim()
        {
            // Drop half of our items randomly on each low memory condition
            var countToRemove = Count / 2;
            var removedCount = 0;
            var random = RandomUtility.Random;

            foreach (var item in this)
            {
                if (random.Next(2) == 1 && TryRemove(item.Key, out _) && removedCount++ > countToRemove)
                {
                    break;
                }
            }

            Log.Write($"Memory cache removed {removedCount} items on low memory");
        }
    }
}
