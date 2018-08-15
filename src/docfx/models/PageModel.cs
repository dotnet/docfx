// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class PageModel
    {
        public string PageType { get; set; }

        public object Content { get; set; }

        public long WordCount { get; set; }

        public string Locale { get; set; }

        public string Toc { get; set; }

        public string Title { get; set; }

        public string HtmlTitle { get; set; }

        public string RedirectionUrl { get; set; }

        public string Id { get; set; }

        public string VersionIndependentId { get; set; }

        public GitUserInfo Author { get; set; }

        public GitUserInfo[] Contributors { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool ShowEdit { get; set; }

        public string EditUrl { get; set; }

        public string ContentUrl { get; set; }

        public string CommitUrl { get; set; }

        public JObject Metadata { get; set; }
    }
}
