// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class FileOutputs
    {
        [JsonProperty(ManifestConstants.BuildManifestItem.OutputHtml)]
        public FileOutput Html { get; set; }

        [JsonProperty(ManifestConstants.BuildManifestItem.OutputRawPageJson)]
        public FileOutput RawPageJson { get; set; }

        [JsonProperty(ManifestConstants.BuildManifestItem.OutputMtaJson)]
        public FileOutput MtaJson { get; set; }

        [JsonProperty(ManifestConstants.BuildManifestItem.OutputResource)]
        public FileOutput Resource { get; set; }

        [JsonProperty(ManifestConstants.BuildManifestItem.OutputJson)]
        public FileOutput TocJson { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> OtherOutputs { get; set; }
    }
}
