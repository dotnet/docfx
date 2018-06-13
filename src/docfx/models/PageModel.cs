// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class PageModel
    {
        public string Content { get; set; }

        public PageMetadata Metadata { get; set; }

        public long WordCount { get; set; }

        public string Locale { get; set; }

        public string TocRelativePath { get; set; }

        public string Gitcommit { get; set; }

        public string OriginalContentGitUrl { get; set; }
    }
}
