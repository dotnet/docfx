// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Incrementals;

    public sealed class DocumentBuildParameters : MarshalByRefObject
    {
        [IncrementalCheckAttribute]
        public FileCollection Files { get; set; }

        [IncrementalCheckAttribute]
        public string OutputBaseDir { get; set; }

        [IncrementalCheckAttribute]
        public ImmutableArray<string> ExternalReferencePackages { get; set; } = ImmutableArray<string>.Empty;

        [IncrementalCheckAttribute]
        public ImmutableArray<string> XRefMaps { get; set; } = ImmutableArray<string>.Empty;

        [IncrementalCheckAttribute]
        public ImmutableDictionary<string, object> Metadata { get; set; } = ImmutableDictionary<string, object>.Empty;

        [IncrementalCheckAttribute]
        public FileMetadata FileMetadata { get; set; }

        [IncrementalCheckAttribute]
        public ImmutableArray<string> PostProcessors { get; set; } = ImmutableArray<string>.Empty;

        [IncrementalCheckAttribute]
        public TemplateManager TemplateManager { get; set; }

        [IncrementalCheckAttribute]
        public ApplyTemplateSettings ApplyTemplateSettings { get; set; }

        public int MaxParallelism { get; set; }

        [IncrementalCheckAttribute]
        public string MarkdownEngineName { get; set; } = "dfm";

        [IncrementalCheckAttribute]
        public ImmutableDictionary<string, object> MarkdownEngineParameters { get; set; } = ImmutableDictionary<string, object>.Empty;

        public string VersionName { get; set; }

        [IncrementalCheckAttribute]
        public string TemplateDir { get; set; }

        public ImmutableDictionary<string, ChangeKindWithDependency> Changes { get; set; } = ImmutableDictionary<string, ChangeKindWithDependency>.Empty;
    }
}
