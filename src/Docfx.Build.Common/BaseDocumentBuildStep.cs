// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Plugins;

namespace Docfx.Build.Common;

public abstract class BaseDocumentBuildStep : IDocumentBuildStep
{
    public abstract string Name { get; }

    public abstract int BuildOrder { get; }

    public virtual IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
    {
        return models;
    }

    public virtual void Build(FileModel model, IHostService host)
    {
    }

    public virtual void Postbuild(ImmutableList<FileModel> models, IHostService host)
    {
    }
}
