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
        public string PageType { get; set; }

        public object Content { get; set; }

        public long WordCount { get; set; }

        public string Locale { get; set; }

        public string TocRel { get; set; }

        public string Title { get; set; }

        public string RawTitle { get; set; }

        public string CanonicalUrl { get; set; }

        public string RedirectUrl { get; set; }

        public string DocumentId { get; set; }

        public string DocumentVersionIndependentId { get; set; }

        public Contributor Author { get; set; }

        public List<Contributor> Contributors { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool OpenToPublicContributors { get; set; }

        public string ContentGitUrl { get; set; }

        public string OriginalContentGitUrl { get; set; }

        public string Gitcommit { get; set; }

        public JObject Metadata { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class Contributor
    {
        public string Name { get; set; }

        public string ProfileUrl { get; set; }

        public string DisplayName { get; set; }

        public string Id { get; set; }
    }
}
