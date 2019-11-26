// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class JsonDiskCache<TError, T> where T : ICacheObject
    {
        private static int s_randomSeed = Environment.TickCount;
        private static ThreadLocal<Random> t_random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref s_randomSeed)));

        private readonly string _cachePath;
        private readonly double _expirationInSeconds;

        private readonly ConcurrentDictionary<object, T> _cache = new ConcurrentDictionary<object, T>();
        private readonly ConcurrentDictionary<object, Lazy<Task<(TError error, T value)>>> _initialRequests = new ConcurrentDictionary<object, Lazy<Task<(TError error, T value)>>>();
        private readonly ConcurrentDictionary<object, Lazy<Task<(TError error, T value)>>> _backgroundUpdates = new ConcurrentDictionary<object, Lazy<Task<(TError error, T value)>>>();

        private volatile bool _needUpdate;

        public JsonDiskCache(string cachePath, TimeSpan expiration)
        {
            _expirationInSeconds = expiration.TotalSeconds;
            _cachePath = Path.GetFullPath(cachePath);

            if (File.Exists(_cachePath))
            {
                var cacheFile = JsonUtility.Deserialize<CacheFile>(
                    ProcessUtility.ReadFile(_cachePath), new FilePath(_cachePath));

                foreach (var item in cacheFile.Items)
                {
                    _cache.TryAdd(item.GetKeys(), item);
                }
            }
        }

        /// <summary>
        /// Gets or update the cache asynchroniously.
        /// The <paramref name="valueFactory"/> can be a long running asynchronious call,
        /// this method only blocks the first time an item is retrieved.
        /// When a cache item expires, this method returns the expired item immediately,
        /// then update the value asynchroniously in the background.
        /// Don't throw exception in <paramref name="valueFactory"/> because of the async update,
        /// the exception may be re-thrown in <see cref="Save"/> method.
        /// </summary>
        public (TError error, T value) GetOrAdd<TKey>(TKey key, Func<TKey, Task<(TError, T)>> valueFactory)
        {
            if (_cache.TryGetValue(key, out var value))
            {
                if (value.Expiry != null && value.Expiry < DateTime.UtcNow)
                {
                    // When the item expired, trigger background update but don't wait for the result
                    _backgroundUpdates.GetOrAdd(key, UpdateDelegate(key, valueFactory)).Value.GetAwaiter();
                }
                return (default, value);
            }

            return _initialRequests.GetOrAdd(key, UpdateDelegate(key, valueFactory)).Value.GetAwaiter().GetResult();
        }

        public async Task<TError[]> Save()
        {
            var result = await Task.WhenAll(_backgroundUpdates.Values.Select(item => item.Value));

            if (_needUpdate)
            {
                var content = JsonUtility.Serialize(new { items = _cache.Values });

                Directory.CreateDirectory(Path.GetDirectoryName(_cachePath));
                ProcessUtility.WriteFile(_cachePath, content);
                _needUpdate = false;
            }

            return result.Select(item => item.error).ToArray();
        }

        private Lazy<Task<(TError, T)>> UpdateDelegate<TKey>(TKey key, Func<TKey, Task<(TError, T)>> valueFactory)
        {
            return new Lazy<Task<(TError, T)>>(async () =>
            {
                var (error, value) = await valueFactory(key);
                if (value != null)
                {
                    value.Expiry = DateTime.UtcNow.AddSeconds(NextEvenDistribution(_expirationInSeconds));

                    foreach (var key in value.GetKeys())
                    {
                        _cache[key] = value;
                    }

                    _needUpdate = true;
                }
                return (error, value);
            });
        }

        private static double NextEvenDistribution(double value)
        {
            return (value / 2) + (t_random.Value.NextDouble() * value / 2);
        }

        private class CacheFile
        {
            public T[] Items { get; set; }
        }
    }
}
