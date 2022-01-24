// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Collections.Concurrent;

internal class ListBuilder<T> where T : notnull
{
    private readonly List<T> _array = new();

    public void Add(T item)
    {
        lock (_array)
        {
            _array.Add(item);
        }
    }

    public void AddRange(IReadOnlyList<T> items)
    {
        lock (_array)
        {
            for (var i = 0; i < items.Count; i++)
            {
                _array.Add(items[i]);
            }
        }
    }

    public IReadOnlyList<T> AsList()
    {
        _array.TrimExcess();
        return _array;
    }
}
