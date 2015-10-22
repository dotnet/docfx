namespace Microsoft.DocAsCode.Utility
{
    using System;
    using System.Collections.Generic;

    public class LruList<T>
    {
        private readonly LinkedList<T> _cache;
        private readonly Dictionary<T, LinkedListNode<T>> _index;
        private readonly int _capacity;
        private readonly Action<T> _onRemoving;

        protected LruList(int capacity, Action<T> onRemoving, IEqualityComparer<T> comparer)
        {
            _cache = new LinkedList<T>();
            _index = new Dictionary<T, LinkedListNode<T>>(capacity, comparer);
            _onRemoving = onRemoving;
            _capacity = capacity;
        }

        public static LruList<T> Create(int capacity, Action<T> onRemoving, IEqualityComparer<T> comparer = null)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "capacity should great than 0.");
            }
            if (onRemoving == null)
            {
                throw new ArgumentNullException(nameof(onRemoving));
            }
            return new LruList<T>(capacity, onRemoving, comparer ?? EqualityComparer<T>.Default);
        }

        public static LruList<T> CreateSynchronized(int capacity, Action<T> onRemoving, IEqualityComparer<T> comparer = null)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "capacity should great than 0.");
            }
            if (onRemoving == null)
            {
                throw new ArgumentNullException(nameof(onRemoving));
            }
            return new SynchronizedLruList(capacity, onRemoving, comparer ?? EqualityComparer<T>.Default);
        }

        protected virtual void AccessNoCheck(T item)
        {
            LinkedListNode<T> node;
            if (_index.TryGetValue(item, out node))
            {
                // reorder
                _cache.Remove(node);
                _index[item] = _cache.AddLast(item);
            }
            else
            {
                if (_cache.Count < _capacity)
                {
                    _index[item] = _cache.AddLast(item);
                }
                else
                {
                    // remove LRU
                    try
                    {
                        _onRemoving(_cache.First.Value);
                    }
                    finally
                    {
                        _index.Remove(_cache.First.Value);
                        _cache.RemoveFirst();
                        _index[item] = _cache.AddLast(item);
                    }
                }
            }
        }

        public void Access(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            AccessNoCheck(item);
        }

        private sealed class SynchronizedLruList : LruList<T>
        {
            private readonly object _syncRoot = new object();

            public SynchronizedLruList(int capacity, Action<T> onRemoving, IEqualityComparer<T> comparer)
                : base(capacity, onRemoving, comparer)
            {
            }

            protected override void AccessNoCheck(T item)
            {
                lock (_syncRoot)
                {
                    base.AccessNoCheck(item);
                }
            }
        }
    }
}
