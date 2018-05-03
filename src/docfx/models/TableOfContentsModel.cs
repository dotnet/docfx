// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsModel
    {
        [JsonProperty(PropertyName = "toc_title")]
        public string TocTitle { get; set; }

        [JsonProperty(PropertyName = "href")]
        public string Href { get; set; }

        [JsonProperty(PropertyName = "pdf_href")]
        public string PdfHref { get; set; }

        [JsonProperty(PropertyName = "pdf_name")]
        public string PdfName { get; set; }

        public List<TableOfContentsModel> Children;
    }
}
