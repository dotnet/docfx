// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
