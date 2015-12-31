// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    [JsonConverter(typeof(FileMetadataPairsConverter))]
    public class FileMetadataPairs
    {
        // Order matters, the latter one overrides the former one
        private List<FileMetadataPairsItem> _items;

        public IReadOnlyList<FileMetadataPairsItem> Items
        {
            get
            {
                return _items.AsReadOnly();
            }
        }

        public FileMetadataPairs(List<FileMetadataPairsItem> items)
        {
            _items = items;
        }

        public FileMetadataPairs(FileMetadataPairsItem item)
        {
            _items = new List<FileMetadataPairsItem>() { item };
        }

        public FileMetadataPairsItem this[int index]
        {
            get
            {
                return _items[index];
            }
        }

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }
    }
}
