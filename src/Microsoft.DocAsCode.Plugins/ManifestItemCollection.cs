// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;

    public class ManifestItemCollection : ObservableCollection<ManifestItem>
    {
        public ManifestItemCollection() { }

        public ManifestItemCollection(IEnumerable<ManifestItem> collection)
            : base(collection) { }

        #region Overrides

        protected override void ClearItems()
        {
            var list = this.ToList();
            base.ClearItems();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, list));
        }

        #endregion

        public void AddRange(IEnumerable<ManifestItem> collection)
        {
            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public int RemoveAll(Predicate<ManifestItem> match)
        {
            int count = 0;
            for (int i = Count - 1; i >= 0; i--)
            {
                if (match(this[i]))
                {
                    RemoveAt(i);
                    count++;
                }
            }
            return count;
        }
    }
}
