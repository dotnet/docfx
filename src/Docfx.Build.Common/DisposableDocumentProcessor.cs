// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Common;

public abstract class DisposableDocumentProcessor : IDocumentProcessor, IDisposable
{
    public abstract string Name { get; }

    public abstract IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

    public abstract ProcessingPriority GetProcessingPriority(FileAndType file);

    public abstract FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata);

    public abstract SaveResult Save(FileModel model);

    public void Dispose()
    {
        if (BuildSteps != null)
        {
            foreach (var buildStep in BuildSteps)
            {
                Logger.LogVerbose($"Disposing build step {buildStep.Name} ...");
                (buildStep as IDisposable)?.Dispose();
            }
        }
    }

    // TODO: implement update href in each plugin
    public virtual void UpdateHref(FileModel model, IDocumentBuildContext context)
    {
    }
}
