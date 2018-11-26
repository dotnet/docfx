// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class TocModel
    {
        [JsonProperty("toc_title")]
        public string Title { get; set; }

        [JsonProperty("relative_path_in_depot")]
        public string HtmlFilePath { get; set; }

        [JsonProperty("external_link")]
        public string ExternalLink { get; set; }

        [JsonProperty("children")]
        public IList<TocModel> Children { get; set; }
    }
}
