// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.ExternalPackageGenerators.Msdn
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Caching;
    using System.Threading.Tasks;

    internal sealed class Cache<T>
        where T : class
    {
        private readonly Dictionary<string, Task<T>> _shortIdTaskMap = new Dictionary<string, Task<T>>();
        private readonly MemoryCache _cache;
        private readonly Func<string, Task<T>> _loader;
        private readonly CacheItemPolicy _policy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(5) };

        public Cache(string name, Func<string, Task<T>> loader)
        {
            _cache = new MemoryCache(name);
            _loader = loader;
        }

        public async Task<T> GetAsync(string key)
        {
            var result = _cache.Get(key) as T;
            if (result == null)
            {
                Task<T> task;
                bool ownTask = false;
                lock (_shortIdTaskMap)
                {
                    if (!_shortIdTaskMap.TryGetValue(key, out task))
                    {
                        task = _loader(key);
                        _shortIdTaskMap[key] = task;
                        ownTask = true;
                    }
                }
                try
                {
                    result = await task;
                    if (ownTask)
                    {
                        _cache.Set(key, result, _policy);
                    }
                }
                finally
                {
                    if (ownTask)
                    {
                        lock (_shortIdTaskMap)
                        {
                            _shortIdTaskMap.Remove(key);
                        }
                    }
                }
            }
            return result;
        }
    }
}
