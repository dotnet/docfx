// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class PageModel
    {
        public string SchemaType { get; set; }

        public object Content { get; set; }

        public long WordCount { get; set; }

        public string Locale { get; set; }

        public string TocRel { get; set; }

        public string Title { get; set; }

        [JsonProperty(PropertyName = "rawTitle")]
        public string RawTitle { get; set; }

        public string CanonicalUrl { get; set; }

        public string RedirectUrl { get; set; }

        public string DocumentId { get; set; }

        public string DocumentVersionIndependentId { get; set; }

        public Contributor Author { get; set; }

        public List<Contributor> Contributors { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string ContentGitUrl { get; set; }

        public string OriginalContentGitUrl { get; set; }

        public string OriginalContentGitUrlTemplate { get; set; }

        public string Gitcommit { get; set; }

        public bool Bilingual { get; set; }

        public List<string> Monikers { get; set; }

        [JsonExtensionData]
        public JObject Metadata { get; set; }
    }
}
