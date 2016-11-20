// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Plugins;

    internal class PrebuildBuildPhaseHandlerWithIncremental : PrebuildBuildPhaseHandler, IPhaseHandler
    {
        public IncrementalBuildContext IncrementalContext { get; }

        public BuildVersionInfo LastBuildVersionInfo
        {
            get { return IncrementalContext.LastBuildVersionInfo; }
        }

        public BuildVersionInfo CurrentBuildVersionInfo
        {
            get { return IncrementalContext.CurrentBuildVersionInfo; }
        }

        public PrebuildBuildPhaseHandlerWithIncremental(DocumentBuildContext context) : base(context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            IncrementalContext = context.IncrementalBuildContext;
        }

        public override void PreHandle(List<HostService> hostServices)
        {
            base.PreHandle(hostServices);
            foreach (var hostService in hostServices.Where(h => h.ShouldTraceIncrementalInfo))
            {
                hostService.DependencyGraph = CurrentBuildVersionInfo.Dependency;
                using (new LoggerPhaseScope("RegisterDependencyTypeFromProcessor", true))
                {
                    hostService.RegisterDependencyType();
                }
            }
            Logger.RegisterListener(CurrentBuildVersionInfo.BuildMessage.GetListener());
        }

        public override void PostHandle(List<HostService> hostServices)
        {
            foreach (var h in hostServices.Where(h => h.CanIncrementalBuild))
            {
                foreach (var file in from pair in IncrementalContext.GetModelLoadInfo(h)
                                     where pair.Value == null
                                     select pair.Key)
                {
                    LastBuildVersionInfo.BuildMessage.Replay(file);
                }
            }
            foreach (var h in hostServices.Where(h => h.ShouldTraceIncrementalInfo))
            {
                h.SaveIntermediateModel(IncrementalContext);
            }
            Logger.UnregisterListener(CurrentBuildVersionInfo.BuildMessage.GetListener());
            base.PostHandle(hostServices);
        }
    }
}
