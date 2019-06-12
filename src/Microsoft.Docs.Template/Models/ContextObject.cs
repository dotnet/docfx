// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System.ComponentModel;

namespace Microsoft.Docs.Build
{
    [DataSchema]
    [JsonObject(Description = "The schema for context object")]
    public sealed class ContextObject
    {
        [JsonProperty("brand")]
        [Description("The brand to be used for the content. Depending on the brand, this may override the UHF header. Not required. Example: azure")]
        public string Brand { get; set; }

        [JsonProperty("breadcrumb_path")]
        [Description("The absolute URL or relative location (relative to the context file) to use for the breadcrumb path file. Overrides the docset value. Example: ./powershell/breadcrumb.json")]
        public string BreadcrumbPath { get; set; }

        [JsonProperty("chromeless")]
        [Description("Whether or not to use chromeless rendering. Only used for Hands-On Learning tutorials.")]
        public bool Chromeless { get; set; }

        [JsonProperty("searchScope")]
        [Description("A list of the search scopes to use with the content.")]
        public string[] SearchScope { get; set; }

        [JsonProperty("toc_rel")]
        [Description("The absolute URL or relative location (relative to the context file) to use for the table of contents file. Overrides the docset value. Example: ../toc.yml")]
        public string TocRel { get; set; }

        [JsonProperty("uhfheaderId")]
        [Description("The UHFHeaderID to use for the content. Overrides the default docset value. Example: MSDocsHeader-Dynamics365")]
        public string UhfHeaderId { get; set; }
    }
}
