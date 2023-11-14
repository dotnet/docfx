// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.HtmlToPdf;

public class FileOutputs
{
    [JsonProperty(ManifestConstants.BuildManifestItem.OutputHtml)]
    [JsonPropertyName(ManifestConstants.BuildManifestItem.OutputHtml)]
    public FileOutput Html { get; set; }

    [JsonProperty(ManifestConstants.BuildManifestItem.OutputRawPageJson)]
    [JsonPropertyName(ManifestConstants.BuildManifestItem.OutputRawPageJson)]
    public FileOutput RawPageJson { get; set; }

    [JsonProperty(ManifestConstants.BuildManifestItem.OutputMtaJson)]
    [JsonPropertyName(ManifestConstants.BuildManifestItem.OutputMtaJson)]
    public FileOutput MtaJson { get; set; }

    [JsonProperty(ManifestConstants.BuildManifestItem.OutputResource)]
    [JsonPropertyName(ManifestConstants.BuildManifestItem.OutputResource)]
    public FileOutput Resource { get; set; }

    [JsonProperty(ManifestConstants.BuildManifestItem.OutputJson)]
    [JsonPropertyName(ManifestConstants.BuildManifestItem.OutputJson)]
    public FileOutput TocJson { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> OtherOutputs { get; set; }
}
