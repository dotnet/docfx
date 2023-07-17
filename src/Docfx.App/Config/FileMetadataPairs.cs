// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx;

/// <summary>
/// FileMetadataPairs.
/// </summary>
/// <see cref="BuildJsonConfig.FileMetadata"/>
/// <see cref="MergeJsonItemConfig.FileMetadata"/>
[Serializable]
[JsonConverter(typeof(FileMetadataPairsConverter))]
internal class FileMetadataPairs
{
    // Order matters, the latter one overrides the former one
    private List<FileMetadataPairsItem> _items;

    /// <summary>
    /// Gets FileMetadataPairs items.
    /// </summary>
    public IReadOnlyList<FileMetadataPairsItem> Items
    {
        get
        {
            return _items.AsReadOnly();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMetadataPairs"/> class.
    /// </summary>
    public FileMetadataPairs(List<FileMetadataPairsItem> items)
    {
        _items = items;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMetadataPairs"/> class.
    /// </summary>
    public FileMetadataPairs(FileMetadataPairsItem item)
    {
        _items = new List<FileMetadataPairsItem> { item };
    }

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    public FileMetadataPairsItem this[int index]
    {
        get
        {
            return _items[index];
        }
    }

    /// <summary>
    /// Gets the number of elements.
    /// </summary>
    public int Count
    {
        get
        {
            return _items.Count;
        }
    }
}
