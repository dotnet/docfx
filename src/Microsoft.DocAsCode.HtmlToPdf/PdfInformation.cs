// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
