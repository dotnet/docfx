// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsItem
    {
        public SourceInfo<string> Name { get; set; }

        public string DisplayName { get; set; }

        public SourceInfo<string> Href { get; set; }

        public SourceInfo<string> TopicHref { get; set; }

        public SourceInfo<string> TocHref { get; set; }

        public string Homepage { get; set; }

        public SourceInfo<string> Uid { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Expanded { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool MaintainContext { get; set; }

        public IReadOnlyList<string> Monikers { get; set; } = Array.Empty<string>();

        public List<TableOfContentsItem> Items { get; set; } = new List<TableOfContentsItem>();

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }

        [JsonIgnore]
        public Document Document { get; set; }

        public TableOfContentsItem(TableOfContentsItem item)
        {
            Name = item.Name;
            DisplayName = item.DisplayName;
            Href = item.Href;
            TopicHref = item.TopicHref;
            TocHref = item.TocHref;
            Homepage = item.Homepage;
            Uid = item.Uid;
            Expanded = item.Expanded;
            MaintainContext = item.MaintainContext;
            ExtensionData = item.ExtensionData;
            Items = item.Items;
            Document = item.Document;
        }

        public TableOfContentsItem() { }
    }
}
