// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class PreviewJsonConfig
    {
        [JsonProperty("buildSourceFolder")]
        public string BuildSourceFolder { get; set; }

        [JsonProperty("buildOutputSubFolder")]
        public string BuildOutputSubFolder { get; set; }

        [JsonProperty("markupTagType")]
        public string MarkupTagType { get; set; }

        [JsonProperty("markupClassName")]
        public string MarkupClassName { get; set; }

        [JsonProperty("outputFolder")]
        public string OutputFolder { get; set; }

        [JsonProperty("pageRefreshFunctionName")]
        public string PageRefreshFunctionName { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }

        [JsonProperty("references")]
        public Dictionary<string, string> References { get; set; }

        [JsonProperty("tocMetadataName")]
        public string TocMetadataName { get; set; }
    }
}
