// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Plugins;

public class LlmsTextOptions
{
    /// <summary>
    /// The title of the project or site, used as H1.
    /// Required section.
    /// </summary>
    [JsonProperty("title")]
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// A short summary of the project, displayed as a blockquote.
    /// </summary>
    [JsonProperty("summary")]
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    /// <summary>
    /// Additional details about the project, displayed as regular text.
    /// </summary>
    [JsonProperty("details")]
    [JsonPropertyName("details")]
    public string Details { get; set; }

    /// <summary>
    /// Main documentation sections as a dictionary of section name to list of links
    /// </summary>
    [JsonProperty("sections")]
    [JsonPropertyName("sections")]
    public Dictionary<string, List<LlmsTextLink>> Sections { get; set; }
}