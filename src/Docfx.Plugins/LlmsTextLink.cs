// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Plugins;

public class LlmsTextLink
{
    /// <summary>
    /// The title of the link
    /// </summary>
    [JsonProperty("title")]
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// The URL of the link
    /// </summary>
    [JsonProperty("url")]
    [JsonPropertyName("url")]
    public string Url { get; set; }

    /// <summary>
    /// Optional description of the link
    /// </summary>
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }
}