// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

using Newtonsoft.Json;

namespace Docfx;

/// <summary>
/// MergeJsonItemConfig.
/// </summary>
[Serializable]
public class MergeJsonItemConfig
{
    /// <summary>
    /// Defines the files to merge.
    /// </summary>
    [JsonProperty("content")]
    public FileMapping Content { get; set; }

    /// <summary>
    /// Defines the output folder of the generated merge files.
    /// </summary>
    [JsonProperty("dest")]
    public string Destination { get; set; }

    /// <summary>
    /// Contains metadata that will be applied to every file, in key-value pair format.
    /// </summary>
    [JsonProperty("globalMetadata")]
    [JsonConverter(typeof(JObjectDictionaryToObjectDictionaryConverter))]
    public Dictionary<string, object> GlobalMetadata { get; set; }

    /// <summary>
    /// Metadata that applies to some specific files.
    /// The key is the metadata name.
    /// For each item of the value:
    ///     The key is the glob pattern to match the files.
    ///     The value is the value of the metadata.
    /// </summary>
    [JsonProperty("fileMetadata")]
    public Dictionary<string, FileMetadataPairs> FileMetadata { get; set; }

    /// <summary>
    /// Metadata that applies to toc files.
    /// </summary>
    [JsonProperty("tocMetadata")]
    public ListWithStringFallback TocMetadata { get; set; }
}
