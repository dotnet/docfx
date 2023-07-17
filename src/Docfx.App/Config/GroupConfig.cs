// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx;

/// <summary>
/// Group configuration.
/// </summary>
[Serializable]
internal class GroupConfig
{
    /// <summary>
    /// Defines the output folder of the generated build files.
    /// </summary>
    [JsonProperty("dest")]
    public string Destination { get; set; }

    /// <summary>
    /// Specifies the tags of xrefmap.
    /// </summary>
    [JsonProperty("xrefTags")]
    public ListWithStringFallback XrefTags { get; set; }

    /// <summary>
    /// Extension metadata.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
