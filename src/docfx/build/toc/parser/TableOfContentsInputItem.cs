// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsInputItem
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Href { get; set; }

        public string TopicHref { get; set; }

        public string TocHref { get; set; }

        public List<TableOfContentsInputItem> Items { get; set; }

        public static TableOfContentsItem ToTableOfContentsModel(TableOfContentsInputItem inputModel)
        {
            if (inputModel == null)
            {
                return null;
            }

            var decodedHref = inputModel.Href == null ? null : HttpUtility.UrlDecode(inputModel.Href);
            return new TableOfContentsItem
            {
                TocTitle = inputModel.DisplayName ?? inputModel.Name,
                Href = decodedHref?.ToLowerInvariant(),
                Children = inputModel.Items?.Select(l => ToTableOfContentsModel(l)).ToList(),
            };
        }
    }
}
