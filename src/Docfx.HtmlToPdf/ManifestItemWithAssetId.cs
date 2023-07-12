// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.HtmlToPdf
{
    using Newtonsoft.Json;

    using Docfx.Common;
    using Docfx.Plugins;

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
