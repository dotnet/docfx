// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx;

/// <summary>
/// FileMapping supports 3 forms:
/// 1. Array form
///     This form supports multiple file mappings, and also allows additional properties per mapping.
///     e.g.
///     <code>
///     "key": [
///       {"files": ["file1", "file2"], "dest": "dest1"},
///       {"files": "file3", "dest": "dest2"},
///       {"files": ["file4", "file5"], "exclude": ["file5"], "src": "folder1"},
///       {"files": "Example.yml", "src": "v1.0", "dest":"v1.0/api", "group": "v1.0"},
///       {"files": "Example.yml", "src": "v2.0", "dest":"v2.0/api", "group": "v2.0"}
///     ]
///     </code>
/// 2. Compact form
///     This form supports multiple file patterns in an array
///     e.g. <code>projects: ["file1", "file2"]</code>
/// 3. Object form
///     If the Array form contains only one item, it can be shortened to an object
///     e.g. <code>projects: ["file1", "file2"]</code>
/// </summary>
[Newtonsoft.Json.JsonConverter(typeof(FileMappingConverter.NewtonsoftJsonConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(FileMappingConverter.SystemTextJsonConverter))]
public class FileMapping
{
    private readonly List<FileMappingItem> _items = [];

    /// <summary>
    /// Flags to distinguish items are expanded or not.
    /// </summary>
    public bool Expanded { get; set; }

    /// <summary>
    /// File mapping items.
    /// </summary>
    public IReadOnlyList<FileMappingItem> Items
    {
        get { return _items.AsReadOnly(); }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMapping"/> class.
    /// </summary>
    public FileMapping()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMapping"/> class.
    /// </summary>
    public FileMapping(IEnumerable<FileMappingItem> items)
    {
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMapping"/> class.
    /// </summary>
    public FileMapping(FileMappingItem item)
    {
        Add(item);
    }

    /// <summary>
    /// Should not merge FileMappingItems even if they are using the same name, because other properties also matters, e.g. src, exclude.
    /// </summary>
    /// <param name="item"></param>
    public void Add(FileMappingItem item)
    {
        if (item?.Files == null || item.Files.Count == 0) return;

        _items.Add(item);
    }

    /// <summary>
    /// The RootTocPath of the current version, specified in FileMappingItems.
    /// If different FileMappingItems in same version have different RootTocPath, the behavior is undetermined.
    /// </summary>
    public string RootTocPath
    {
        get
        {
            var fileMappingItem = _items.Find(i => !string.IsNullOrEmpty(i.RootTocPath));
            return fileMappingItem?.RootTocPath;
        }
    }
}
