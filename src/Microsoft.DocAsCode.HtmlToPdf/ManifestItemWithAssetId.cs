// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using Newtonsoft.Json;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class ManifestItemWithAssetId : ManifestItem
    {
        [JsonProperty(ManifestConstants.BuildManifestItem.AssetId)]
        public string AssetId { get; set; }

        [JsonProperty(ManifestConstants.BuildManifestItem.SkipNormalization)]
        public bool SkipNormalization { get; set; }

        public override string ToString()
        {
            return JsonUtility.ToJsonString(this);
        }
    }
}
