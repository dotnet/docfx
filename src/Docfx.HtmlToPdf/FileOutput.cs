// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common;
using Newtonsoft.Json;

namespace Docfx.HtmlToPdf;

public class FileOutput
{
    [JsonProperty(ManifestConstants.BuildManifestItem.OutputRelativePath)]
    [JsonPropertyName(ManifestConstants.BuildManifestItem.OutputRelativePath)]
    public string RelativePath { get; set; }

    [JsonProperty(ManifestConstants.BuildManifestItem.OutputLinkToPath)]
    [JsonPropertyName(ManifestConstants.BuildManifestItem.OutputLinkToPath)]
    public string LinkToPath { get; set; }

    [JsonProperty(ManifestConstants.BuildManifestItem.IsRawPage)]
    [JsonPropertyName(ManifestConstants.BuildManifestItem.IsRawPage)]
    public bool IsRawPage { get; set; }

    [JsonProperty(ManifestConstants.BuildManifestItem.SkipPublish, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [JsonPropertyName(ManifestConstants.BuildManifestItem.SkipPublish)]
    public bool SkipPublish { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; }

    public override string ToString()
    {
        return JsonUtility.ToJsonString(this);
    }
}
