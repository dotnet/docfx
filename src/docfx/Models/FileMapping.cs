// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// FileMapping supports three forms:
    /// 1. Object form
    ///     This form supports multiple name-files file mappings, with the property name as the name, and the value as the files.
    ///     e.g. 
    ///     ```
    ///     projects: {
    ///      "name1": ["file1", "file2"],
    ///      "name2": "file3"
    ///     }
    ///     ```
    /// 2. Array form
    ///     This form supports multiple name-files file mappings, and also allows additional properties per mapping.
    ///     e.g. 
    ///     ```
    ///     projects: [
    ///      {name: "name1", files: ["file1", "file2"]},
    ///      {name: "name2", files: "file3"},
    ///      {files:  ["file4", "file5"], exclude: ["file5"]}
    ///     ]
    ///     ```
    /// 3. Compact form
    ///     This form supports multiple file patterns in an array
    ///     e.g. `projects: ["file1", "file2"]`
    /// </summary>
    [JsonConverter(typeof(FileMappingConverter))]
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
