// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    // to-do: include apply templates
    internal class PostbuildPhaseHandler : IPhaseHandler
    {
        private DocumentBuildContext _context;

        public PostbuildPhaseHandler(DocumentBuildContext context)
        {
            _context = context;
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
        }

        public virtual void PreHandle(List<HostService> hostServices)
        {
        }

        public virtual void PostHandle(List<HostService> hostServices)
        {
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
