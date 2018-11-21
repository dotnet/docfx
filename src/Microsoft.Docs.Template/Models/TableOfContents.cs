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

        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "href")]
        public string Href { get; set; }

        [JsonProperty(PropertyName = "tocHref")]
        public string TocHref { get; set; }

        [JsonProperty(PropertyName = "expanded", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Expanded { get; set; }

        [JsonProperty(PropertyName = "maintainContext", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool MaintainContext { get; set; }

        [JsonProperty(PropertyName = "children")]
        public List<TableOfContentsItem> Children { get; set; }

        [JsonProperty(PropertyName = "monikers")]
        public List<string> Monikers { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }
    }
}
