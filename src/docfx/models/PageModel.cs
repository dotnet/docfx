// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class PageModel
    {
        public string Content { get; set; }

        public long WordCount { get; set; }

        public string Locale { get; set; }

        public string TocRelativePath { get; set; }

        public string Title { get; set; }

        public string RedirectionUrl { get; set; }

        public JObject Metadata { get; set; }
    }
}
