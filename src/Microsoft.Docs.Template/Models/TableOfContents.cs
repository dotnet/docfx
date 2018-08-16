// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class TableOfContentsModel
    {
        [JsonProperty(PropertyName = "metadata")]
        public JObject Metadata { get; set; }

        [JsonProperty(PropertyName = "items")]
        public List<TableOfContentsItem> Items { get; set; }
    }

    public class TableOfContentsItem
    {
        [JsonProperty(PropertyName = "toc_title")]
        public string TocTitle { get; set; }

        [JsonProperty(PropertyName = "href")]
        public string Href { get; set; }

        [JsonProperty(PropertyName = "children")]
        public List<TableOfContentsItem> Children { get; set; }

        [JsonExtensionData]
        public JObject Metadata { get; set; }
    }
}
