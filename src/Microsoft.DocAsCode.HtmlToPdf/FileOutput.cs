// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode.Common;

    public class FileOutput
    {
        [JsonProperty(ManifestConstants.BuildManifestItem.OutputRelativePath)]
        public string RelativePath { get; set; }

        [JsonProperty(ManifestConstants.BuildManifestItem.OutputLinkToPath)]
        public string LinkToPath { get; set; }

        [JsonProperty(ManifestConstants.BuildManifestItem.OutputHash)]
        public string Hash { get; set; }

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
}
