// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Plugins;

namespace Docfx.Build.Engine;

internal interface IHostServiceCreator
{
    /// <summary>
    /// Load file into model
    /// </summary>
    /// <returns>
    /// model: the file Model, returns null if loading file failed
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
