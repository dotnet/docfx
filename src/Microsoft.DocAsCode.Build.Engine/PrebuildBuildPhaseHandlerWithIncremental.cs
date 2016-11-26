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

    internal class PrebuildBuildPhaseHandlerWithIncremental : PrebuildBuildPhaseHandler
    {
        public IncrementalBuildContext IncrementalContext { get; }

        public BuildVersionInfo LastBuildVersionInfo { get; }

        public BuildVersionInfo CurrentBuildVersionInfo { get; }

        public BuildMessageInfo LastBuildMessageInfo { get; }

        public BuildMessageInfo CurrentBuildMessageInfo { get; }

        public PrebuildBuildPhaseHandlerWithIncremental(DocumentBuildContext context) : base(context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            IncrementalContext = context.IncrementalBuildContext;
            LastBuildVersionInfo = IncrementalContext.LastBuildVersionInfo;
            LastBuildMessageInfo = GetPhaseMessageInfo(LastBuildVersionInfo?.BuildMessage);
            CurrentBuildVersionInfo = IncrementalContext.CurrentBuildVersionInfo;
            CurrentBuildMessageInfo = GetPhaseMessageInfo(CurrentBuildVersionInfo.BuildMessage);
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
            Logger.RegisterListener(CurrentBuildMessageInfo.GetListener());
        }

        public override void PostHandle(List<HostService> hostServices)
        {
            foreach (var h in hostServices.Where(h => h.CanIncrementalBuild))
            {
                foreach (var file in from pair in IncrementalContext.GetModelLoadInfo(h)
                                     where pair.Value == null
                                     select pair.Key)
                {
                    LastBuildMessageInfo.Replay(file);
                }
            }
            foreach (var h in hostServices.Where(h => h.ShouldTraceIncrementalInfo))
            {
                h.SaveIntermediateModel(IncrementalContext);
            }
            Logger.UnregisterListener(CurrentBuildMessageInfo.GetListener());
            base.PostHandle(hostServices);
        }

        #region Private Methods

        private static BuildMessageInfo GetPhaseMessageInfo(BuildMessage messages)
        {
            if (messages == null)
            {
                return null;
            }

            BuildMessageInfo message;
            if (!messages.TryGetValue(BuildPhase.PreBuildBuild, out message))
            {
                messages[BuildPhase.PreBuildBuild] = message = new BuildMessageInfo();
            }
            return message;
        }

        #endregion
    }
}
