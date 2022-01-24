// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Collections.Concurrent;

internal class ConcurrentHashSet<T> : IEnumerable<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, object?> _dictionary;

    public ConcurrentHashSet() => _dictionary = new();

    public ConcurrentHashSet(IEnumerable<T> source) => _dictionary = new(source.Select(item => new KeyValuePair<T, object?>(item, default)));

    public ConcurrentHashSet(IEqualityComparer<T> comparer) => _dictionary = new(comparer);

    public bool TryAdd(T value) => _dictionary.TryAdd(value, null);

    public bool Contains(T value) => _dictionary.ContainsKey(value);

    public IEnumerator<T> GetEnumerator() => _dictionary.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _dictionary.Keys.GetEnumerator();
}
