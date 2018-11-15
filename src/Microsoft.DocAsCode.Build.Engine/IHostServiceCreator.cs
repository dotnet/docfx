// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Plugins;

    internal interface IHostServiceCreator
    {
        bool ShouldProcessorTraceInfo(IDocumentProcessor processor);

        bool CanProcessorIncremental(IDocumentProcessor processor);

        /// <summary>
        /// Load file into model
        /// </summary>
        /// <returns>
        /// model: the file Model, returns null if no need to load in incremental build or loading file failed
        /// valid: whether loading file succeeds
        /// </returns>
        (FileModel model, bool valid) Load(
            IDocumentProcessor processor,
            ImmutableDictionary<string, object> metadata,
            FileMetadata fileMetadata,
            FileAndType file);

        HostService CreateHostService(
            DocumentBuildParameters parameters,
            TemplateProcessor templateProcessor,
            IMarkdownService markdownService,
            IEnumerable<IInputMetadataValidator> metadataValidator,
            IDocumentProcessor processor,
            IEnumerable<FileAndType> files);
    }
}
