// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Markdig;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsCode.Build.Engine;

public sealed class DocumentBuildParameters : IBuildParameters
{
    public FileCollection Files { get; set; }

    public string OutputBaseDir { get; set; }

    public IReadOnlyDictionary<string, JArray> TagParameters { get; set; }

    public ImmutableArray<string> ExternalReferencePackages { get; set; } = ImmutableArray<string>.Empty;

    public ImmutableArray<string> XRefMaps { get; set; } = ImmutableArray<string>.Empty;

    public ImmutableArray<string> XRefServiceUrls { get; set; } = ImmutableArray<string>.Empty;

    public ImmutableDictionary<string, object> Metadata { get; set; } = ImmutableDictionary<string, object>.Empty;

    public FileMetadata FileMetadata { get; set; }

    public ImmutableArray<string> PostProcessors { get; set; } = ImmutableArray<string>.Empty;

    public TemplateManager TemplateManager { get; set; }

    public ApplyTemplateSettings ApplyTemplateSettings { get; set; }

    public int MaxParallelism { get; set; }

    public int MaxHttpParallelism { get; set; }

    public ImmutableDictionary<string, object> MarkdownEngineParameters { get; set; } = ImmutableDictionary<string, object>.Empty;

    public Func<MarkdownPipelineBuilder, MarkdownPipelineBuilder> ConfigureMarkdig { get; set; }

    public string VersionName { get; set; }

    public string VersionDir { get; set; }

    public GroupInfo GroupInfo { get; set; }

    public List<string> XRefTags { get; set; }

    public string RootTocPath { get; set; }

    public string TemplateDir { get; set; }

    public string CustomLinkResolver { get; set; }

    public int LruSize { get; set; }

    public bool KeepFileLink { get; set; }

    public SitemapOptions SitemapOptions { get; set; }

    public string FALName { get; set; }

    public bool DisableGitFeatures { get; set; }

    public ImmutableArray<FolderRedirectionRule> OverwriteFragmentsRedirectionRules { get; set; }
        = ImmutableArray<FolderRedirectionRule>.Empty;

    public DocumentBuildParameters Clone() =>
        (DocumentBuildParameters)MemberwiseClone();
}
