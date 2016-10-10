// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;

    public sealed class DocumentBuildParameters : MarshalByRefObject
    {
        public FileCollection Files { get; set; }

        public string OutputBaseDir { get; set; }

        [IncrementalCheck]
        public ImmutableArray<string> ExternalReferencePackages { get; set; } = ImmutableArray<string>.Empty;

        [IncrementalCheck]
        public ImmutableArray<string> XRefMaps { get; set; } = ImmutableArray<string>.Empty;

        [IncrementalCheck]
        public ImmutableDictionary<string, object> Metadata { get; set; } = ImmutableDictionary<string, object>.Empty;

        [IncrementalCheck]
        public FileMetadata FileMetadata { get; set; }

        [IncrementalCheck]
        public ImmutableArray<string> PostProcessors { get; set; } = ImmutableArray<string>.Empty;

        public TemplateManager TemplateManager { get; set; }

        // todo : partial properties should check.
        //[IncrementalCheck]
        public ApplyTemplateSettings ApplyTemplateSettings { get; set; }

        public int MaxParallelism { get; set; }

        [IncrementalCheck]
        public string MarkdownEngineName { get; set; } = "dfm";

        [IncrementalCheck]
        public ImmutableDictionary<string, object> MarkdownEngineParameters { get; set; } = ImmutableDictionary<string, object>.Empty;

        public string VersionName { get; set; }

        public string TemplateDir { get; set; }

        public ImmutableDictionary<string, ChangeKindWithDependency> Changes { get; set; } = ImmutableDictionary<string, ChangeKindWithDependency>.Empty;

        public bool ForceRebuild { get; set; }
    }
}
