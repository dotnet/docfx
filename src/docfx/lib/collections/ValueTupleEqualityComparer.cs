// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Collections.Concurrent;

public static class ValueTupleEqualityComparer
{
    public static IEqualityComparer<(T1, T2)> Create<T1, T2>(IEqualityComparer<T1>? comparer1, IEqualityComparer<T2>? comparer2)
    {
        return new EqualityComparer<T1, T2>(comparer1, comparer2);
    }

    private class EqualityComparer<T1, T2> : IEqualityComparer<(T1, T2)>
    {
        private readonly IEqualityComparer<T1> _comparer1;
        private readonly IEqualityComparer<T2> _comparer2;

        public EqualityComparer(IEqualityComparer<T1>? comparer1, IEqualityComparer<T2>? comparer2)
        {
            _comparer1 = comparer1 ?? EqualityComparer<T1>.Default;
            _comparer2 = comparer2 ?? EqualityComparer<T2>.Default;
        }

        public bool Equals((T1, T2) x, (T1, T2) y)
        {
            return _comparer1.Equals(x.Item1, y.Item1) && _comparer2.Equals(x.Item2, y.Item2);
        }

        public int GetHashCode((T1, T2) obj)
        {
            return HashCode.Combine(
                obj.Item1 is null ? 0 : _comparer1.GetHashCode(obj.Item1),
                obj.Item2 is null ? 0 : _comparer2.GetHashCode(obj.Item2));
        }
    }
}
