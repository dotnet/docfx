// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class JsonDiskCache<TError, TKey, TValue> where TValue : ICacheObject<TKey>
    {
        private static int s_randomSeed = Environment.TickCount;
        private static ThreadLocal<Random> t_random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref s_randomSeed)));

        private readonly string _cachePath;
        private readonly double _expirationInSeconds;

        private readonly ConcurrentDictionary<TKey, TValue> _cache;
        private readonly ConcurrentDictionary<TKey, Lazy<Task<TError>>> _backgroundUpdates;

        private volatile bool _needUpdate;

        public JsonDiskCache(string cachePath, TimeSpan expiration, IEqualityComparer<TKey> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<TKey>.Default;
            _cache = new ConcurrentDictionary<TKey, TValue>(comparer);
            _backgroundUpdates = new ConcurrentDictionary<TKey, Lazy<Task<TError>>>(comparer);

            _expirationInSeconds = expiration.TotalSeconds;
            _cachePath = Path.GetFullPath(cachePath);

            if (File.Exists(_cachePath))
            {
                var cacheFile = ProcessUtility.ReadJsonFile<CacheFile>(_cachePath);

                foreach (var item in cacheFile.Items)
                {
                    foreach (var cacheKey in item.GetKeys())
                    {
                        if (cacheKey != null)
                        {
                            _cache.TryAdd(cacheKey, item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets an item from the cache asynchronously, or creates the value if it does not exist.
        /// The <paramref name="valueFactory"/> can be a long running asynchronous call,
        /// this method only blocks the first time an item is retrieved.
        /// When a cache item expires, this method returns the expired item immediately,
        /// then update the value asynchronously in the background.
        /// Don't throw exception in <paramref name="valueFactory"/> because of the async update,
        /// the exception may be re-thrown in <see cref="Save"/> method.
        /// </summary>
        public Task<(TError error, TValue value)> GetOrAdd(TKey key, Func<TKey, Task<(TError, TValue)>> valueFactory)
        {
            return GetOrAdd(key, async aKey =>
            {
                var (error, value) = await valueFactory(aKey);
                return (error, new[] { value });
            });
        }

        public async Task<(TError error, TValue value)> GetOrAdd(TKey key, Func<TKey, Task<(TError, IEnumerable<TValue>)>> valueFactory)
        {
            if (_cache.TryGetValue(key, out var value))
            {
                if (HasExpired(value))
                {
                    // When the item expired, trigger background update but don't wait for the result
                    Update(key, valueFactory).GetAwaiter();
                }
                return (default, value);
            }

            var error = await Update(key, valueFactory);
            _cache.TryGetValue(key, out value);
            return (error, value);
        }

        public async Task<TError[]> Save()
        {
            var result = await Task.WhenAll(_backgroundUpdates.Values.Select(item => item.Value));

            if (_needUpdate)
            {
                var content = JsonUtility.Serialize(new { items = _cache.Values.Distinct() });

                Directory.CreateDirectory(Path.GetDirectoryName(_cachePath));
                ProcessUtility.WriteFile(_cachePath, content);
                _needUpdate = false;
            }

            return result.Where(error => error != null).ToArray();
        }

        private Task<TError> Update(TKey key, Func<TKey, Task<(TError, IEnumerable<TValue>)>> valueFactory)
        {
            return _backgroundUpdates.GetOrAdd(key, UpdateDelegate(key, valueFactory)).Value;
        }

        private Lazy<Task<TError>> UpdateDelegate(TKey key, Func<TKey, Task<(TError, IEnumerable<TValue>)>> valueFactory)
        {
            return new Lazy<Task<TError>>(async () =>
            {
                var (error, values) = await valueFactory(key);
                if (values != null)
                {
                    foreach (var value in values)
                    {
                        if (value != null)
                        {
                            value.UpdatedAt = GetRandomUpdatedAt();

                            foreach (var cacheKey in value.GetKeys())
                            {
                                if (cacheKey != null)
                                {
                                    _cache[cacheKey] = value;
                                }
                            }
                            _needUpdate = true;
                        }
                    }
                }
                return error;
            });
        }

        private static DateTime GetRandomUpdatedAt()
        {
            return DateTime.UtcNow.AddMilliseconds(1000.0 * t_random.Value.NextDouble());
        }

        private bool HasExpired(TValue value)
        {
            var updatedAt = value.UpdatedAt ?? ((DateTime)(value.UpdatedAt = GetRandomUpdatedAt()));
            var expiry = (0.5 + (updatedAt.Millisecond / 2000.0)) * _expirationInSeconds;

            return updatedAt.AddSeconds(expiry) < DateTime.UtcNow;
        }

        private class CacheFile
        {
            public TValue[] Items { get; set; } = Array.Empty<TValue>();
        }
    }
}
