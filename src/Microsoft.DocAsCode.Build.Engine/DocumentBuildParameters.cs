// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json.Linq;

    public sealed class DocumentBuildParameters : MarshalByRefObject, IBuildParameters
    {
        [IncrementalIgnore]
        public FileCollection Files { get; set; }

        [IncrementalIgnore]
        public string OutputBaseDir { get; set; }

        [IncrementalIgnore]
        public IReadOnlyDictionary<string, JArray> TagParameters { get; set; }

        public ImmutableArray<string> ExternalReferencePackages { get; set; } = ImmutableArray<string>.Empty;

        public ImmutableArray<string> XRefMaps { get; set; } = ImmutableArray<string>.Empty;

        public ImmutableArray<string> XRefServiceUrls { get; set; } = ImmutableArray<string>.Empty;

        public ImmutableDictionary<string, object> Metadata { get; set; } = ImmutableDictionary<string, object>.Empty;

        public FileMetadata FileMetadata { get; set; }

        [IncrementalIgnore]
        public ImmutableArray<string> PostProcessors { get; set; } = ImmutableArray<string>.Empty;

        [IncrementalIgnore]
        public TemplateManager TemplateManager { get; set; }

        [IncrementalIgnore]
        public ApplyTemplateSettings ApplyTemplateSettings { get; set; }

        [IncrementalIgnore]
        public int MaxParallelism { get; set; }

        [IncrementalIgnore]
        public int MaxHttpParallelism { get; set; }

        public string MarkdownEngineName { get; set; } = "dfm";

        [IncrementalIgnore]
        public ImmutableDictionary<string, object> MarkdownEngineParameters { get; set; } = ImmutableDictionary<string, object>.Empty;

        [IncrementalIgnore]
        public string VersionName { get; set; }

        [IncrementalIgnore]
        public string VersionDir { get; set; }

        [IncrementalIgnore]
        public GroupInfo GroupInfo { get; set; }

        [IncrementalIgnore]
        public List<string> XRefTags { get; set; }

        public string RootTocPath { get; set; }

        [IncrementalIgnore]
        public string TemplateDir { get; set; }

        [IncrementalIgnore]
        public ImmutableDictionary<string, ChangeKindWithDependency> Changes { get; set; }

        [IncrementalIgnore]
        public bool ForceRebuild { get; set; }

        [IncrementalIgnore]
        public bool ForcePostProcess { get; set; }

        public string CustomLinkResolver { get; set; }

        [IncrementalIgnore]
        public int LruSize { get; set; }

        [IncrementalIgnore]
        public bool KeepFileLink { get; set; }

        [IncrementalIgnore]
        public SitemapOptions SitemapOptions { get; set; }

        [IncrementalIgnore]
        public string SchemaLicense { get; set; }

        public string FALName { get; set; }

        public bool DisableGitFeatures { get; set; }

        public ImmutableArray<FolderRedirectionRule> OverwriteFragmentsRedirectionRules { get; set; }
            = ImmutableArray<FolderRedirectionRule>.Empty;

        public DocumentBuildParameters Clone() =>
            (DocumentBuildParameters)MemberwiseClone();
    }
}
