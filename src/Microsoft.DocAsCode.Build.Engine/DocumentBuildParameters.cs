// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Plugins;

    public sealed class DocumentBuildParameters : MarshalByRefObject
    {
        [IncrementalIgnore]
        public FileCollection Files { get; set; }

        [IncrementalIgnore]
        public string OutputBaseDir { get; set; }

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

        public string MarkdownEngineName { get; set; } = "dfm";

        [IncrementalIgnore]
        public ImmutableDictionary<string, object> MarkdownEngineParameters { get; set; } = ImmutableDictionary<string, object>.Empty;

        [IncrementalIgnore]
        public string VersionName { get; set; }

        [IncrementalIgnore]
        public string VersionDir { get; set; }

        [IncrementalIgnore]
        public GroupInfo GroupInfo { get; set; }

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

        public DocumentBuildParameters Clone() =>
            (DocumentBuildParameters)MemberwiseClone();
    }
}
