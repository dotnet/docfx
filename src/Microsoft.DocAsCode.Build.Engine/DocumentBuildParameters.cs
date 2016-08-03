// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Immutable;

    public sealed class DocumentBuildParameters : MarshalByRefObject
    {
        public FileCollection Files { get; set; }
        public string OutputBaseDir { get; set; }
        public ImmutableArray<string> ExternalReferencePackages { get; set; } = ImmutableArray<string>.Empty;
        public ImmutableArray<string> XRefMaps { get; set; } = ImmutableArray<string>.Empty;
        public ImmutableDictionary<string, object> Metadata { get; set; } = ImmutableDictionary<string, object>.Empty;
        public FileMetadata FileMetadata { get; set; }
        public ImmutableArray<string> PostProcessors { get; set; } = ImmutableArray<string>.Empty;
        public TemplateManager TemplateManager { get; set; }
        public ApplyTemplateSettings ApplyTemplateSettings { get; set; }
        public int MaxParallelism { get; set; }
        public string MarkdownEngineName { get; set; } = "dfm";
        public ImmutableDictionary<string, object> MarkdownEngineParameters { get; set; } = ImmutableDictionary<string, object>.Empty;
        public string VersionName { get; set; }
        public string TemplateDir { get; set; }
    }
}
