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
    internal class JsonDiskCache<TError, TKey, TValue>
        where TError : class
        where TKey : notnull
        where TValue : class, ICacheObject<TKey>
    {
        private static int s_randomSeed = Environment.TickCount;
        private static ThreadLocal<Random> t_random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref s_randomSeed)));

        private readonly string _cachePath;
        private readonly double _expirationInSeconds;
        private readonly Func<TValue, TValue, TValue>? _resolveConflict;
        private readonly ConcurrentDictionary<TKey, TValue> _cache;
        private readonly ConcurrentDictionary<TKey, Lazy<Task<TError?>>> _backgroundUpdates;

        private volatile bool _needUpdate;

        public JsonDiskCache(
            string cachePath,
            TimeSpan expiration,
            IEqualityComparer<TKey>? comparer = null,
            Func<TValue, TValue, TValue>? resolveConflict = null)
        {
            comparer ??= EqualityComparer<TKey>.Default;
            _resolveConflict = resolveConflict;
            _cache = new ConcurrentDictionary<TKey, TValue>(comparer);
            _backgroundUpdates = new ConcurrentDictionary<TKey, Lazy<Task<TError?>>>(comparer);

            _expirationInSeconds = expiration.TotalSeconds;
            _cachePath = Path.GetFullPath(cachePath);

            if (File.Exists(_cachePath))
            {
                var cacheFile = ProcessUtility.ReadJsonFile<CacheFile>(_cachePath);

                foreach (var item in cacheFile.Items)
                {
                    UpdateCache(item);
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
        public (TError? error, TValue? value) GetOrAdd(TKey key, Func<TKey, Task<(TError?, TValue?)>> valueFactory)
        {
            return GetOrAdd(key, async aKey =>
            {
                var (error, value) = await valueFactory(aKey);
                return (error, value is null ? Array.Empty<TValue>() : new[] { value });
            });
        }

        public (TError? error, TValue? value) GetOrAdd(TKey key, Func<TKey, Task<(TError?, IEnumerable<TValue>)>> valueFactory)
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

            var error = Update(key, valueFactory).GetAwaiter().GetResult();
            _cache.TryGetValue(key, out value);
            return (error, value);
        }

        public TError[] Save()
        {
            var result = Task.WhenAll(_backgroundUpdates.Values.Select(item => item.Value)).GetAwaiter().GetResult();

            if (_needUpdate)
            {
                var content = JsonUtility.Serialize(new { items = _cache.Values.Distinct().Where(value => !HasExpired(value)) });

                Directory.CreateDirectory(Path.GetDirectoryName(_cachePath));
                ProcessUtility.WriteFile(_cachePath, content);
                _needUpdate = false;
            }

            return (from error in result where error != null select error).ToArray();
        }

        private Task<TError?> Update(TKey key, Func<TKey, Task<(TError?, IEnumerable<TValue>)>> valueFactory)
        {
            return _backgroundUpdates.GetOrAdd(key, UpdateDelegate(key, valueFactory)).Value;
        }

        private Lazy<Task<TError?>> UpdateDelegate(TKey key, Func<TKey, Task<(TError?, IEnumerable<TValue>)>> valueFactory)
        {
            return new Lazy<Task<TError?>>(async () =>
            {
                var (error, values) = await valueFactory(key);
                if (values != null)
                {
                    foreach (var value in values)
                    {
                        if (value != null)
                        {
                            value.UpdatedAt = GetRandomUpdatedAt();
                            UpdateCache(value);
                            _needUpdate = true;
                        }
                    }
                }
                return error;
            });
        }

        private void UpdateCache(TValue value)
        {
            foreach (var cacheKey in value.GetKeys())
            {
                _cache[cacheKey] = _resolveConflict != null && _cache.TryGetValue(cacheKey, out var existingValue) && !HasExpired(existingValue)
                   ? _resolveConflict(value, existingValue)
                   : value;
            }
        }

        private static DateTime GetRandomUpdatedAt()
        {
            return DateTime.UtcNow.AddMilliseconds(1000.0 * t_random.Value!.NextDouble());
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
