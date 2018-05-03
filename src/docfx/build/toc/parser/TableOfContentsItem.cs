// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.using System;

using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsItem
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Href { get; set; }

        public List<TableOfContentsItem> Items { get; set; }

        public static TableOfContentsModel ToTableOfContentsModel(TableOfContentsItem inputModel)
        {
            if (inputModel == null)
            {
                return null;
            }

            // todo: pdf name and pdf href
            var decodedHref = inputModel.Href == null ? null : HttpUtility.UrlDecode(inputModel.Href);
            return new TableOfContentsModel
            {
                TocTitle = inputModel.DisplayName ?? inputModel.Name,
                Href = decodedHref?.ToLowerInvariant(),
                Children = inputModel.Items?.Select(l => ToTableOfContentsModel(l)).ToList(),
            };
        }
    }
}
