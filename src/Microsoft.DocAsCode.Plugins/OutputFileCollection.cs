// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;

    public class OutputFileCollection : Dictionary<string, OutputFileInfo>
    {
        public new OutputFileInfo this[string key]
        {
            get { return base[key]; }
            set { base[key] = value; }
        }

        public new IEqualityComparer<string> Comparer => base.Comparer;

        public new int Count => base.Count;

        public new ICollection<string> Keys => base.Keys;

        public new ICollection<OutputFileInfo> Values => base.Values;

        public new void Add(string key, OutputFileInfo value) => base.Add(key, value);

        public new void Clear() => base.Clear();

        public new bool ContainsKey(string key) => base.ContainsKey(key);

        public new bool ContainsValue(OutputFileInfo value) => base.ContainsValue(value);

        public new IEnumerator<KeyValuePair<string, OutputFileInfo>> GetEnumerator() => base.GetEnumerator();

        public new bool Remove(string key) => base.Remove(key);

        public new bool TryGetValue(string key, out OutputFileInfo value) => base.TryGetValue(key, out value);
    }
}
