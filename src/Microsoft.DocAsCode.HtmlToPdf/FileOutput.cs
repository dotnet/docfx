// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Common;
using Newtonsoft.Json;

namespace Microsoft.DocAsCode.HtmlToPdf;

public class FileOutput
{
    [JsonProperty(ManifestConstants.BuildManifestItem.OutputRelativePath)]
    public string RelativePath { get; set; }

    [JsonProperty(ManifestConstants.BuildManifestItem.OutputLinkToPath)]
    public string LinkToPath { get; set; }

    [JsonProperty(ManifestConstants.BuildManifestItem.IsRawPage)]
    public bool IsRawPage { get; set; }

    [JsonProperty(ManifestConstants.BuildManifestItem.SkipPublish, DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool SkipPublish { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; }

    public override string ToString()
    {
        return JsonUtility.ToJsonString(this);
    }
}
