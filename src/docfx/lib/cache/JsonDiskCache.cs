// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal class JsonDiskCache<TError, TKey, TValue>
    where TError : class
    where TKey : notnull
    where TValue : class, ICacheObject<TKey>
{
    private readonly string _cachePath;
    private readonly double _expirationInSeconds;
    private readonly Func<TValue, TValue, TValue>? _resolveConflict;
    private readonly ConcurrentDictionary<TKey, TValue> _cache;
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TError?>>> _backgroundUpdates;

    private volatile bool _needUpdate;

    public JsonDiskCache(
        string cachePath, TimeSpan expiration, IEqualityComparer<TKey>? comparer = null, Func<TValue, TValue, TValue>? resolveConflict = null)
    {
        comparer ??= EqualityComparer<TKey>.Default;
        _resolveConflict = resolveConflict;
        _cache = new(comparer);
        _backgroundUpdates = new(comparer);

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
                // Update expired items synchronously, for pull request build, items never expire.
                Update(key, valueFactory).GetAwaiter().GetResult();
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
            Directory.CreateDirectory(Path.GetDirectoryName(_cachePath) ?? ".");
            ProcessUtility.WriteJsonFile(_cachePath, new { items = _cache.Values.Distinct().Where(value => !HasExpired(value)).ToArray() });
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
        return DateTime.UtcNow.AddMilliseconds(1000.0 * RandomUtility.Random.NextDouble());
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
