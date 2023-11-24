// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.Plugins;

public interface IDocumentProcessor
{
    string Name { get; }
    IEnumerable<IDocumentBuildStep> BuildSteps { get; }
    ProcessingPriority GetProcessingPriority(FileAndType file);
    FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata);

    SaveResult Save(FileModel model);

    void UpdateHref(FileModel model, IDocumentBuildContext context);
}
