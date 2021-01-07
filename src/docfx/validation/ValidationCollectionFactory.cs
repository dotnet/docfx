using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build
{
    public class ValidationCollectionFactory : ICollectionFactory
    {
        public IProducerConsumerCollection<T> CreateCollection<T>()
        {
            return new ScopedValidationCollection<T>();
        }
    }

    internal class ScopedValidationCollection<T> : IProducerConsumerCollection<T>
    {
        private readonly Scoped<ConcurrentBag<T>> _innerCollection = new();

        public int Count => _innerCollection.Value.Count;

        public bool IsSynchronized { get; }

        public object SyncRoot
        {
            get
            {
                var collection = _innerCollection.Value as ICollection;
                return collection.SyncRoot;
            }
        }

        public void CopyTo(T[] array, int index)
        {
            _innerCollection.Value.CopyTo(array, index);
        }

        public void CopyTo(Array array, int index)
        {
            var collection = _innerCollection.Value as ICollection;
            collection.CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _innerCollection.Value.GetEnumerator();
        }

        public T[] ToArray()
        {
            return _innerCollection.Value.ToArray();
        }

        public bool TryAdd(T item)
        {
            Watcher.Write(() => _innerCollection.Value.Add(item));
            return true;
        }

        public bool TryTake([MaybeNullWhen(false)] out T item)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerCollection.Value.GetEnumerator();
        }
    }
}
