// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.HtmlToPdf;

public class PdfInformation
{
    [JsonProperty("docset_name")]
    public string DocsetName { get; set; }

    [JsonProperty("asset_id")]
    public string AssetId { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("toc_files")]
    public ICollection<string> TocFiles { get; set; }
}
