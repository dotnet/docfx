// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class LinkPhaseHandler : IPhaseHandler
    {
        public string Name => nameof(LinkPhaseHandler);

        public BuildPhase Phase => BuildPhase.Link;

        public DocumentBuildContext Context { get; }

        public TemplateProcessor TemplateProcessor { get; }

        public LinkPhaseHandler(DocumentBuildContext context, TemplateProcessor templateProcessor)
        {
            Context = context;
            TemplateProcessor = templateProcessor;
        }

        public void Handle(List<HostService> hostServices, int maxParallelism)
        {
            Postbuild(hostServices, maxParallelism);
            ProcessManifest(hostServices, maxParallelism);
        }

        public void Postbuild(List<HostService> hostServices, int maxParallelism)
        {
            foreach (var hostService in hostServices)
            {
                using (new LoggerPhaseScope(hostService.Processor.Name, LogLevel.Verbose))
                {
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}: Postbuilding...");
                    using (new LoggerPhaseScope("Postbuild", LogLevel.Verbose))
                    {
                        Postbuild(hostService);
                    }
                }
            }
        }

        public void ProcessManifest(List<HostService> hostServices, int maxParallelism)
        {
            if (Context != null)
            {
                var manifestProcessor = new ManifestProcessor(hostServices, Context, TemplateProcessor);
                manifestProcessor.Process();
            }
        }

        #region Private Methods

        private static void Postbuild(HostService hostService)
        {
            BuildPhaseUtility.RunBuildSteps(
                hostService.Processor.BuildSteps,
                buildStep =>
                {
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Postbuilding...");
                    using (new LoggerPhaseScope(buildStep.Name, LogLevel.Verbose))
                    {
                        buildStep.Postbuild(hostService.Models, hostService);
                    }
                });
        }

        #endregion
    }
}
