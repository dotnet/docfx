// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Docfx.Common;

public static class CollectionExtensions
{
    public static IEnumerable<TResult> Merge<TItem, TResult>(
        this IReadOnlyList<IEnumerable<TItem>> sources,
        IComparer<TItem> comparer,
        Func<List<TItem>, TResult> merger)
    {
        var enumerators = new EnumeratorInfo<TItem>[sources.Count];
        try
        {
            for (int i = 0; i < sources.Count; i++)
            {
                enumerators[i] = new EnumeratorInfo<TItem>(sources[i]?.OrderBy(x => x, comparer));
            }
            var indexes = new List<int>(sources.Count);
            while (MoveNext(enumerators, indexes, comparer, merger, out TResult result))
            {
                yield return result;
                indexes.Clear();
            }
        }
        finally
        {
            foreach (var item in enumerators)
            {
                item.Dispose();
            }
        }
    }

    private static bool MoveNext<TItem, TResult>(EnumeratorInfo<TItem>[] enumerators, List<int> indexes, IComparer<TItem> comparer, Func<List<TItem>, TResult> merger, out TResult result)
    {
        for (int i = 0; i < enumerators.Length; i++)
        {
            if (enumerators[i].MoveNext())
            {
                if (indexes.Count == 0)
                {
                    indexes.Add(i);
                }
                else
                {
                    var c = comparer.Compare(enumerators[indexes[0]].Current, enumerators[i].Current);
                    if (c > 0)
                    {
                        foreach (var x in indexes)
                        {
                            enumerators[x].Rollback();
                        }
                        indexes.Clear();
                        indexes.Add(i);
                    }
                    else if (c == 0)
                    {
                        indexes.Add(i);
                    }
                    else if (c < 0)
                    {
                        enumerators[i].Rollback();
                    }
                }
            }
        }
        if (indexes.Count == 0)
        {
            result = default;
            return false;
        }
        result = merger((from i in indexes select enumerators[i].Current).ToList());
        return true;
    }

    private sealed class EnumeratorInfo<T>
        : IEnumerator<T>
    {
        private bool _rollbacked;

        public EnumeratorInfo(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                Enumerator = null;
                Eof = true;
            }
            else
            {
                Enumerator = enumerable.GetEnumerator();
                Eof = false;
            }
        }

        public IEnumerator<T> Enumerator { get; }

        public bool Eof { get; private set; }

        public T Current => Eof ? default : Enumerator.Current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            Enumerator?.Dispose();
        }

        public bool MoveNext()
        {
            if (Eof)
            {
                return false;
            }
            if (_rollbacked)
            {
                _rollbacked = false;
                return true;
            }
            var result = Enumerator.MoveNext();
            if (result)
            {
                return true;
            }
            Eof = true;
            return false;
        }

        public void Rollback()
        {
            if (Eof)
            {
                throw new InvalidOperationException("Cannot rollback when eof is true.");
            }
            if (_rollbacked)
            {
                throw new InvalidOperationException("Cannot rollback twice.");
            }
            _rollbacked = true;
        }

        public void Reset() => throw new NotImplementedException();
    }
}
