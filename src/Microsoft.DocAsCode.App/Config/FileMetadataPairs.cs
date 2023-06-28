// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode;

[Serializable]
[JsonConverter(typeof(FileMetadataPairsConverter))]
internal class FileMetadataPairs
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
        _items = new List<FileMetadataPairsItem> { item };
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
