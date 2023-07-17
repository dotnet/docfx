// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.Plugins;

public interface IDocumentBuildStep
{
    string Name { get; }
    int BuildOrder { get; }
    IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host);
    void Build(FileModel model, IHostService host);
    void Postbuild(ImmutableList<FileModel> models, IHostService host);
}
