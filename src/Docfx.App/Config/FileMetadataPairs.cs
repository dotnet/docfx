// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx;

/// <summary>
/// FileMetadataPairs.
/// </summary>
/// <see cref="BuildJsonConfig.FileMetadata"/>
/// <see cref="MergeJsonItemConfig.FileMetadata"/>
[Newtonsoft.Json.JsonConverter(typeof(FileMetadataPairsConverter.NewtonsoftJsonConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(FileMetadataPairsConverter.SystemTextJsonConverter))]
internal class FileMetadataPairs
{
    // Order matters, the latter one overrides the former one
    private readonly List<FileMetadataPairsItem> _items;

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
    public FileMetadataPairs(IEnumerable<FileMetadataPairsItem> items)
    {
        _items = items.ToList();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMetadataPairs"/> class.
    /// </summary>
    public FileMetadataPairs(FileMetadataPairsItem item)
    {
        _items = [item];
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
