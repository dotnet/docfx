// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class SystemMetadata
{
    public string? Locale { get; set; }

    public string? Author { get; set; }

    public string? BreadcrumbPath { get; set; }

    [JsonProperty("_tocRel")]
    public string? TocRel { get; set; }

    public string? CanonicalUrl { get; set; }

    public string? RedirectUrl { get; set; }

    public string? DocumentId { get; set; }

    public string? DocumentVersionIndependentId { get; set; }

    public string? Robots { get; set; }

    [JsonProperty("_op_gitContributorInformation")]
    public ContributionInfo? ContributionInfo { get; set; }

    public string?[]? GithubContributors { get; set; }

    public string? UpdatedAt { get; set; }

    public string? ContentGitUrl { get; set; }

    public string? OriginalContentGitUrl { get; set; }

    public string? OriginalContentGitUrlTemplate { get; set; }

    public string? Gitcommit { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public MonikerList Monikers { get; set; }

    public string? SiteName { get; set; }

    public string? DepotName { get; set; }

    [JsonProperty("_path")]
    public string? Path { get; set; }

    [JsonProperty("_rel")]
    public string? Rel { get; set; }

    [JsonProperty("_op_canonicalUrlPrefix")]
    public string? CanonicalUrlPrefix { get; set; }

    [JsonProperty("_op_pdfUrlPrefixTemplate")]
    public string? PdfUrlPrefixTemplate { get; set; }

    public ExternalXrefSpec[]? Xrefs { get; set; }
}
