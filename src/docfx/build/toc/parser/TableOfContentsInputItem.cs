// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsInputItem
    {
        [JsonRequired]
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Href { get; set; }

        public string TopicHref { get; set; }

        public string TocHref { get; set; }

        public string Uid { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }

        public bool MaintainContext { get; set; }

        public bool Expanded { get; set; }

        [MinLength(1)]
        public List<TableOfContentsInputItem> Items { get; set; }

        public List<string> Monikers { get; set; }

        public TableOfContentsItem ToTableOfContentsModel()
        {
            return new TableOfContentsItem
            {
                TocTitle = this.Name,
                DisplayName = this.DisplayName,
                Href = this.Href,
                TocHref = this.TocHref, // only breadcrumb toc will set the toc href
                MaintainContext = this.MaintainContext,
                Expanded = this.Expanded,
                ExtensionData = this.ExtensionData,
                Children = this.Items?.Select(l => l.ToTableOfContentsModel())?.ToList(),
                Monikers = this.Monikers,
            };
        }
    }
}
