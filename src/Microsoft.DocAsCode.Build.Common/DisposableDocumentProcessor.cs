// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

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
}
