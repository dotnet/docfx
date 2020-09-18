// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsNode
    {
        public SourceInfo<string?> Name { get; set; }

        public string? DisplayName { get; set; }

        public SourceInfo<string?> Href { get; set; }

        public SourceInfo<string?> TopicHref { get; set; }

        public SourceInfo<string?> TocHref { get; set; }

        public string? Homepage { get; set; }

        public SourceInfo<string?> Uid { get; set; }

        public SourceInfo<LandingPageType?> LandingPageType { get; set; }

        public static bool ShouldSerializeLandingPageType() => false;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Expanded { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool MaintainContext { get; set; }

        public string? SplitItemsBy { get; set; }

        public static bool ShouldSerializeSplitItemsBy() => false;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public MonikerList Monikers { get; set; }

        public List<SourceInfo<TableOfContentsNode>> Items { get; set; } = new List<SourceInfo<TableOfContentsNode>>();

        public string[] Children { get; set; } = Array.Empty<string>();

        public static bool ShouldSerializeChildren() => false;

        [JsonExtensionData]
        public JObject ExtensionData { get; set; } = new JObject();

        [JsonIgnore]
        public FilePath? Document { get; set; }

        public TableOfContentsNode() { }

        public TableOfContentsNode(TableOfContentsNode item)
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
            SplitItemsBy = item.SplitItemsBy;
            Items = item.Items;
            Document = item.Document;
            Children = item.Children;
            LandingPageType = item.LandingPageType;
        }

        public static void SeperatedExpandableClickableNode(TableOfContentsNode toc)
        {
            if (toc == null || toc.Items == null || toc.Items.Count == 0)
            {
                return;
            }

            foreach (var child in toc.Items)
            {
                SeperatedExpandableClickableNode(child);
            }

            if (!string.IsNullOrEmpty(toc.Uid) || !string.IsNullOrEmpty(toc.Href))
            {
                var overview = toc.Clone();
                overview.Name = overview.Name.With("Overview");
                toc.Items.Insert(0, new SourceInfo<TableOfContentsNode>(overview));
                toc.Uid = toc.Uid.With(null);
                toc.Href = toc.Href.With(null);
            }
        }

        public TableOfContentsNode Clone()
        {
            var cloned = (TableOfContentsNode)this.MemberwiseClone();
            if (cloned.Items != null && cloned.Items.Count > 0)
            {
                cloned.Items = CloneItems();
            }
            return cloned;
        }

        private List<SourceInfo<TableOfContentsNode>> CloneItems()
        {
            var items = this.Items;
            var clonedItems = new List<SourceInfo<TableOfContentsNode>>();
            if (items != null && items.Count > 0)
            {
                foreach (var item in items)
                {
                    var newItem = new SourceInfo<TableOfContentsNode>(item.Value.Clone(), item.Source);
                    clonedItems.Add(newItem);
                }
            }
            return clonedItems;
        }
    }
}
