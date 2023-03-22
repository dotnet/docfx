// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Microsoft.DocAsCode.Plugins;

public interface IDocumentProcessor
{
    string Name { get; }
    IEnumerable<IDocumentBuildStep> BuildSteps { get; }
    ProcessingPriority GetProcessingPriority(FileAndType file);
    FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata);

    // TODO: rename
    SaveResult Save(FileModel model);

    void UpdateHref(FileModel model, IDocumentBuildContext context);
}
