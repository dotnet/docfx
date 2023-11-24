// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.HtmlToPdf;

public class PdfInformation
{
    [JsonProperty("docset_name")]
    [JsonPropertyName("docset_name")]
    public string DocsetName { get; set; }

    [JsonProperty("asset_id")]
    [JsonPropertyName("asset_id")]
    public string AssetId { get; set; }

    [JsonProperty("version")]
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonProperty("toc_files")]
    [JsonPropertyName("toc_files")]
    public ICollection<string> TocFiles { get; set; }
}
