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

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }

        public bool MaintainContext { get; set; }

        public bool Expanded { get; set; }

        [MinLength(1)]
        public List<TableOfContentsInputItem> Items { get; set; }

        public List<string> Monikers { get; set; }

        public static TableOfContentsItem ToTableOfContentsModel(TableOfContentsInputItem inputModel, MonikerComparer comparer)
        {
            if (inputModel == null)
            {
                return null;
            }

            var children = inputModel.Items?.Select(l => ToTableOfContentsModel(l, comparer));
            var childrenMonikers = children?.SelectMany(child => child.Monikers ?? new List<string>());

            var monikers = (childrenMonikers == null ? inputModel.Monikers : childrenMonikers.Union(inputModel.Monikers)).ToHashSet(StringComparer.OrdinalIgnoreCase).ToList();
            monikers.Sort(comparer);
            return new TableOfContentsItem
            {
                TocTitle = inputModel.Name,
                DisplayName = inputModel.DisplayName,
                Href = inputModel.Href,
                TocHref = inputModel.TocHref, // only breadcrumb toc will set the toc href
                MaintainContext = inputModel.MaintainContext,
                Expanded = inputModel.Expanded,
                ExtensionData = inputModel.ExtensionData,
                Children = children?.ToList(),
                Monikers = monikers,
            };
        }
    }
}
