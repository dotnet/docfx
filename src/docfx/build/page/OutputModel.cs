// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class OutputModel : InputMetadata
    {
        [JsonIgnore]
        public string SchemaType { get; set; }

        public object Content { get; set; }

        public string Conceptual { get; set; }

        [JsonProperty("wordCount")]
        public long WordCount { get; set; }

        public string TocRel { get; set; }

        [JsonProperty(PropertyName = "rawTitle")]
        public string RawTitle { get; set; }

        public string CanonicalUrl { get; set; }

        public string RedirectUrl { get; set; }

        public string DocumentId { get; set; }

        public string DocumentVersionIndependentId { get; set; }

        [JsonProperty("_op_gitContributorInformation")]
        public ContributionInfo ContributionInfo { get; set; }

        public string UpdatedAt { get; set; }

        public string ContentGitUrl { get; set; }

        public string OriginalContentGitUrl { get; set; }

        public string OriginalContentGitUrlTemplate { get; set; }

        public string Gitcommit { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool EnableLocSxs { get; set; }

        public List<string> Monikers { get; set; }

        public string SiteName { get; set; }

        public string DepotName { get; set; }

        [JsonProperty("_path")]
        public string Path { get; set; }

        [JsonProperty("_op_canonicalUrlPrefix")]
        public string CanonicalUrlPrefix { get; set; }

        [JsonProperty("_op_pdfUrlPrefixTemplate")]
        public string PdfUrlPrefixTemplate { get; set; }

        // todo: remove this if `enable_loc_sxs` works well
        public string BilingualType => EnableLocSxs ? "hover over" : null;
    }
}
