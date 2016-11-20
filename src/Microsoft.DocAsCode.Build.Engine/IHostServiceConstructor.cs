// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Plugins;

    internal interface IHostServiceConstructor
    {
        bool ShouldProcessorTraceInfo(IDocumentProcessor processor);
        bool CanProcessorIncremental(IDocumentProcessor processor);
        void PostConstruct(HostService hostService, IEnumerable<FileAndType> files);
        FileModel Load(IDocumentProcessor processor, ImmutableDictionary<string, object> metadata, FileMetadata fileMetadata, FileAndType file);
        HostService ConstructHostService(
            DocumentBuildParameters parameters,
            TemplateProcessor templateProcessor,
            IMarkdownService markdownService,
            IEnumerable<IInputMetadataValidator> metadataValidator,
            IDocumentProcessor processor,
            IEnumerable<FileAndType> files);
    }
}
