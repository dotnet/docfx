// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build;

internal class ScopedConcurrentBag<T> : IProducerConsumerCollection<T>
{
    private readonly Scoped<ConcurrentBag<T>> _innerCollection = new();

    public int Count => _innerCollection.Value.Count;

    public bool IsSynchronized { get; }

    public object SyncRoot => ((ICollection)_innerCollection.Value).SyncRoot;

    public void CopyTo(T[] array, int index) => _innerCollection.Value.CopyTo(array, index);

    public void CopyTo(Array array, int index) => ((ICollection)_innerCollection.Value).CopyTo(array, index);

    public IEnumerator<T> GetEnumerator() => _innerCollection.Value.GetEnumerator();

    public T[] ToArray() => _innerCollection.Value.ToArray();

    public bool TryTake([MaybeNullWhen(false)] out T item) => throw new NotSupportedException();

    IEnumerator IEnumerable.GetEnumerator() => _innerCollection.Value.GetEnumerator();

    public bool TryAdd(T item)
    {
        Watcher.Write(() => _innerCollection.Value.Add(item));
        return true;
    }
}
