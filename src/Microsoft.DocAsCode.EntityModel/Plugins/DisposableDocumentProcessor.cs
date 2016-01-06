namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

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
                    Logger.LogInfo($"Disposing build step {buildStep.Name} ...");
                    (buildStep as IDisposable)?.Dispose();
                }
            }
        }
    }
}
