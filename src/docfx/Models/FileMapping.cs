// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// FileMapping supports two forms:
    /// 1. Array form
    ///     This form supports multiple file mappings, and also allows additional properties per mapping.
    ///     e.g. 
    ///     ```
    ///     "key": [
    ///       {"files": ["file1", "file2"], "dest": "dest1"},
    ///       {"files": "file3", "dest": "dest2"},
    ///       {"files": ["file4", "file5"], "exclude": ["file5"], "src": "folder1"},
    ///       {"files": "Example.yml", "src": "v1.0", "dest":"v1.0/api", "version": "v1.0"},
    ///       {"files": "Example.yml", "src": "v2.0", "dest":"v2.0/api", "version": "v2.0"}
    ///     ]
    ///     ```
    /// 2. Compact form
    ///     This form supports multiple file patterns in an array
    ///     e.g. `projects: ["file1", "file2"]`
    /// </summary>
    [JsonConverter(typeof(FileMappingConverter))]
    [Serializable]
    public class FileMapping
    {
        private List<FileMappingItem> _items = new List<FileMappingItem>();

        public bool Expanded { get; set; }

        public IReadOnlyList<FileMappingItem> Items
        {
            get { return _items.AsReadOnly(); }
        }

        public FileMapping() : base() { }

        public FileMapping(IEnumerable<FileMappingItem> items)
        {
            foreach (var item in items) this.Add(item);
        }
        public FileMapping(FileMappingItem item)
        {
            this.Add(item);
        }

        /// <summary>
        /// Should not merge FileMappingItems even if they are using the same name, because other propertes also matters, e.g. cwd, exclude.
        /// </summary>
        /// <param name="item"></param>
        public void Add(FileMappingItem item)
        {
            if (item == null || item.Files == null || item.Files.Count == 0) return;

            _items.Add(item);
        }
    }
}
