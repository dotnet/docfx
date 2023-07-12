// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

using Newtonsoft.Json;

namespace Docfx;

[Serializable]
internal class MergeJsonItemConfig
{
    [JsonProperty("content")]
    public FileMapping Content { get; set; }

    [JsonProperty("dest")]
    public string Destination { get; set; }

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

    [JsonProperty("tocMetadata")]
    public ListWithStringFallback TocMetadata { get; set; }
}
