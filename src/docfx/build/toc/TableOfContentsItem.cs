// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class TableOfContentsItem
    {
        public SourceInfo<string> Name { get; set; }

        public string DisplayName { get; set; }

        public SourceInfo<string> Href { get; set; }

        public SourceInfo<string> TopicHref { get; set; }

        public SourceInfo<string> TocHref { get; set; }

        public SourceInfo<string> Uid { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Expanded { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool MaintainContext { get; set; }

        public List<string> Monikers { get; set; }

        public List<TableOfContentsItem> Items { get; set; } = new List<TableOfContentsItem>();

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }
    }
}
