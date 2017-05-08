// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class Manifest
    {
        [JsonProperty(ManifestConstants.BuildManifest.PublishOnlyMetadata, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, object> PublishOnlyMetadata { get; set; }

        [JsonProperty(ManifestConstants.BuildManifest.TypeMapping)]
        public Dictionary<string, string> TypeMapping { get; set; }

        [JsonProperty(ManifestConstants.BuildManifest.Files)]
        public ManifestItemWithAssetId[] Files { get; set; }
    }
}
