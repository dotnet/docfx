// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class ManifestItemCollection : List<ManifestItem>
    {
        public ManifestItemCollection() { }

        public ManifestItemCollection(IEnumerable<ManifestItem> collection)
            : base(collection) { }

        public new ManifestItem this[int index]
        {
            get { return base[index]; }
            set { base[index] = value; }
        }

        [Obsolete]
        public new int Capacity
        {
            get { return base.Capacity; }
            set { base.Capacity = value; }
        }

        public new int Count => base.Count;

        public new void Add(ManifestItem item) => base.Add(item);

        public new void AddRange(IEnumerable<ManifestItem> collection) => base.AddRange(collection);

        [Obsolete]
        public new ReadOnlyCollection<ManifestItem> AsReadOnly() => base.AsReadOnly();

        [Obsolete]
        public new int BinarySearch(ManifestItem item) => base.BinarySearch(item);

        [Obsolete]
        public new int BinarySearch(ManifestItem item, IComparer<ManifestItem> comparer) => base.BinarySearch(item, comparer);

        [Obsolete]
        public new int BinarySearch(int index, int count, ManifestItem item, IComparer<ManifestItem> comparer) => base.BinarySearch(index, count, item, comparer);

        public new void Clear() => base.Clear();

        public new bool Contains(ManifestItem item) => base.Contains(item);

        [Obsolete]
        public new List<TOutput> ConvertAll<TOutput>(Converter<ManifestItem, TOutput> converter) => base.ConvertAll(converter);

        [Obsolete]
        public new void CopyTo(ManifestItem[] array) => base.CopyTo(array);

        public new void CopyTo(ManifestItem[] array, int arrayIndex) => base.CopyTo(array, arrayIndex);

        [Obsolete]
        public new void CopyTo(int index, ManifestItem[] array, int arrayIndex, int count) => base.CopyTo(index, array, arrayIndex, count);

        [Obsolete]
        public new bool Exists(Predicate<ManifestItem> match) => base.Exists(match);

        [Obsolete]
        public new ManifestItem Find(Predicate<ManifestItem> match) => base.Find(match);

        [Obsolete]
        public new List<ManifestItem> FindAll(Predicate<ManifestItem> match) => base.FindAll(match);

        [Obsolete]
        public new int FindIndex(Predicate<ManifestItem> match) => base.FindIndex(match);

        [Obsolete]
        public new int FindIndex(int startIndex, Predicate<ManifestItem> match) => base.FindIndex(startIndex, match);

        [Obsolete]
        public new int FindIndex(int startIndex, int count, Predicate<ManifestItem> match) => base.FindIndex(startIndex, count, match);

        [Obsolete]
        public new ManifestItem FindLast(Predicate<ManifestItem> match) => base.FindLast(match);

        [Obsolete]
        public new int FindLastIndex(Predicate<ManifestItem> match) => base.FindLastIndex(match);

        [Obsolete]
        public new int FindLastIndex(int startIndex, Predicate<ManifestItem> match) => base.FindLastIndex(startIndex, match);

        [Obsolete]
        public new int FindLastIndex(int startIndex, int count, Predicate<ManifestItem> match) => base.FindLastIndex(startIndex, count, match);

        [Obsolete]
        public new void ForEach(Action<ManifestItem> action) => base.ForEach(action);

        public new IEnumerator<ManifestItem> GetEnumerator() => base.GetEnumerator();

        [Obsolete]
        public new List<ManifestItem> GetRange(int index, int count) => base.GetRange(index, count);

        public new int IndexOf(ManifestItem item) => base.IndexOf(item);

        [Obsolete]
        public new int IndexOf(ManifestItem item, int index) => base.IndexOf(item, index);

        [Obsolete]
        public new int IndexOf(ManifestItem item, int index, int count) => base.IndexOf(item, index, count);

        public new void Insert(int index, ManifestItem item) => base.Insert(index, item);

        [Obsolete]
        public new void InsertRange(int index, IEnumerable<ManifestItem> collection) => base.InsertRange(index, collection);

        [Obsolete]
        public new int LastIndexOf(ManifestItem item) => base.LastIndexOf(item);

        [Obsolete]
        public new int LastIndexOf(ManifestItem item, int index) => base.LastIndexOf(item, index);

        [Obsolete]
        public new int LastIndexOf(ManifestItem item, int index, int count) => base.LastIndexOf(item, index, count);

        public new bool Remove(ManifestItem item) => base.Remove(item);

        public new int RemoveAll(Predicate<ManifestItem> match) => base.RemoveAll(match);

        public new void RemoveAt(int index) => base.RemoveAt(index);

        [Obsolete]
        public new void RemoveRange(int index, int count) => base.RemoveRange(index, count);

        [Obsolete]
        public new void Reverse() => base.Reverse();

        [Obsolete]
        public new void Reverse(int index, int count) => base.Reverse(index, count);

        [Obsolete]
        public new void Sort() => base.Sort();

        [Obsolete]
        public new void Sort(Comparison<ManifestItem> comparison) => base.Sort(comparison);

        [Obsolete]
        public new void Sort(IComparer<ManifestItem> comparer) => base.Sort(comparer);

        [Obsolete]
        public new void Sort(int index, int count, IComparer<ManifestItem> comparer) => base.Sort(index, count, comparer);

        [Obsolete]
        public new ManifestItem[] ToArray() => base.ToArray();

        [Obsolete]
        public new void TrimExcess() => base.TrimExcess();

        [Obsolete]
        public new bool TrueForAll(Predicate<ManifestItem> match) => base.TrueForAll(match);
    }
}
