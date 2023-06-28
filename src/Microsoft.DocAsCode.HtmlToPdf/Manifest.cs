// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
