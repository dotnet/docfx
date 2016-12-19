// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public static class BuildPhaseUtility
    {
        public static void RunBuildSteps(IEnumerable<IDocumentBuildStep> buildSteps, Action<IDocumentBuildStep> action)
        {
            if (buildSteps != null)
            {
                foreach (var buildStep in buildSteps.OrderBy(step => step.BuildOrder))
                {
                    action(buildStep);
                }
            }
        }

        public static BuildMessageInfo GetPhaseMessageInfo(BuildMessage messages, BuildPhase phase)
        {
            if (messages == null)
            {
                return null;
            }

            BuildMessageInfo message;
            if (!messages.TryGetValue(phase, out message))
            {
                messages[phase] = message = new BuildMessageInfo();
            }
            return message;
        }
    }
}
