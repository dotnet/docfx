// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    internal class PostbuildPhaseHandler : IPhaseHandler
    {
        public string Name => nameof(PostbuildPhaseHandler);

        public DocumentBuildContext Context { get; }

        public TemplateProcessor TemplateProcessor { get; }

        public PostbuildPhaseHandler(DocumentBuildContext context, TemplateProcessor templateProcessor)
        {
            Context = context;
            TemplateProcessor = templateProcessor;
        }

        public void Handle(List<HostService> hostServices, int maxParallelism)
        {
            foreach (var hostService in hostServices)
            {
                using (new LoggerPhaseScope(hostService.Processor.Name, true))
                {
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}: Postbuilding...");
                    using (new LoggerPhaseScope("Postbuild", true))
                    {
                        Postbuild(hostService);
                    }
                }
            }

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
                    using (new LoggerPhaseScope(buildStep.Name, true))
                    {
                        buildStep.Postbuild(hostService.Models, hostService);
                    }
                });
        }

        #endregion
    }
}
