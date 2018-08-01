// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsItem
    {
        [JsonProperty(PropertyName = "toc_title")]
        public string TocTitle { get; set; }

        [JsonProperty(PropertyName = "href")]
        public string Href { get; set; }

        [JsonProperty(PropertyName = "metadata")]
        public JObject Metadata { get; set; }

        public List<TableOfContentsItem> Children;
    }
}
