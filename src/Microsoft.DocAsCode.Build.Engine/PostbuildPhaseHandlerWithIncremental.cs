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

    internal class PostbuildPhaseHandlerWithIncremental : PostbuildPhaseHandler, IPhaseHandler
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

        public PostbuildPhaseHandlerWithIncremental(DocumentBuildContext context) : base(context)
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
            UpdateHostServices(hostServices);
        }

        public override void PostHandle(List<HostService> hostServices)
        {
            IncrementalContext.UpdateBuildVersionInfoPerDependencyGraph();
            base.PostHandle(hostServices);
        }

        #region Private Methods

        private void UpdateHostServices(IEnumerable<HostService> hostServices)
        {
            foreach (var hostService in hostServices.Where(h => h.CanIncrementalBuild))
            {
                hostService.ReloadUnloadedModels(IncrementalContext, BuildPhase.PostBuild);
            }
        }

        #endregion
    }
}
