// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Collections.Concurrent;

internal class DictionaryBuilder<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary = new();

    public bool TryAdd(TKey key, TValue value)
    {
        lock (_dictionary)
        {
            return _dictionary.TryAdd(key, value);
        }
    }

    public IReadOnlyDictionary<TKey, TValue> AsDictionary()
    {
        _dictionary.TrimExcess();
        return _dictionary;
    }
}
