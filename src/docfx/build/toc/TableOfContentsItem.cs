// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class TableOfContentsItem
    {
        [JsonRequired]
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Href { get; set; }

        public string TopicHref { get; set; }

        public string TocHref { get; set; }

        public string Uid { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Expanded { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool MaintainContext { get; set; }

        public List<string> Monikers { get; set; }

        [MinLength(1)]
        public List<TableOfContentsItem> Items { get; set; } = new List<TableOfContentsItem>();

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }
    }
}
