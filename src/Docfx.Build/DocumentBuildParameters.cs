// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Plugins;
using Markdig;

namespace Docfx.Build.Engine;

public class DocumentBuildParameters
{
    public FileCollection Files { get; set; }

    public string OutputBaseDir { get; set; }

    public ImmutableArray<string> XRefMaps { get; set; } = [];

    public ImmutableDictionary<string, object> Metadata { get; set; } = ImmutableDictionary<string, object>.Empty;

    public FileMetadata FileMetadata { get; set; }

    public ImmutableArray<string> PostProcessors { get; set; } = [];

    public TemplateManager TemplateManager { get; set; }

    public ApplyTemplateSettings ApplyTemplateSettings { get; set; }

    public int MaxParallelism { get; set; }

    public MarkdownServiceProperties MarkdownEngineParameters { get; set; } = new();

    public Func<MarkdownPipelineBuilder, MarkdownPipelineBuilder> ConfigureMarkdig { get; set; }

    public string VersionName { get; set; }

    public string VersionDir { get; set; }

    public GroupInfo GroupInfo { get; set; }

    public string RootTocPath { get; set; }

    public string TemplateDir { get; set; }

    public string CustomLinkResolver { get; set; }

    public SitemapOptions SitemapOptions { get; set; }

    public bool DisableGitFeatures { get; set; }

    public DocumentBuildParameters Clone() =>
        (DocumentBuildParameters)MemberwiseClone();
}
