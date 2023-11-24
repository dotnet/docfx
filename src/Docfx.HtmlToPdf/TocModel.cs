// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.HtmlToPdf;

public class TocModel
{
    [JsonProperty("toc_title")]
    [JsonPropertyName("toc_title")]
    public string Title { get; set; }

    [JsonProperty("relative_path_in_depot")]
    [JsonPropertyName("relative_path_in_depot")]
    public string HtmlFilePath { get; set; }

    [JsonProperty("external_link")]
    [JsonPropertyName("external_link")]
    public string ExternalLink { get; set; }

    [JsonProperty("children")]
    [JsonPropertyName("children")]
    public IList<TocModel> Children { get; set; }
}
