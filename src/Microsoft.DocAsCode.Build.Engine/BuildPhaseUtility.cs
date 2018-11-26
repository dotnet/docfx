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

            if (!messages.TryGetValue(phase, out BuildMessageInfo message))
            {
                messages[phase] = message = new BuildMessageInfo();
            }
            return message;
        }

        internal static void RelayBuildMessage(IncrementalBuildContext context, IEnumerable<HostService> hostServices, BuildPhase phase)
        {
            var falseSet = new HashSet<string>(from h in hostServices
                                               where !h.CanIncrementalBuild
                                               from f in h.Models
                                               select f.OriginalFileAndType.File,
                                                  FilePathComparer.OSPlatformSensitiveStringComparer);
            var fileSet = new HashSet<string>(from h in hostServices
                                              where h.CanIncrementalBuild
                                              from f in GetFilesToRelayMessages(context, h)
                                              where !falseSet.Contains(f)
                                              select f,
                                              FilePathComparer.OSPlatformSensitiveStringComparer);

            var lastBuildMessageInfo = GetPhaseMessageInfo(context.LastBuildVersionInfo?.BuildMessage, phase);
            foreach (var file in fileSet)
            {
                lastBuildMessageInfo.Replay(file);
            }
        }

        private static IEnumerable<string> GetFilesToRelayMessages(IncrementalBuildContext context, HostService hs)
        {
            var files = new HashSet<string>();
            var cvi = context.CurrentBuildVersionInfo;
            foreach (var f in hs.GetUnloadedModelFiles(context))
            {
                files.Add(f);

                // warnings from token file won't be delegated to article, so we need to add it manually
                var key = ((RelativePath)f).GetPathFromWorkingFolder();
                foreach (var item in cvi.Dependency.GetAllIncludeDependencyFrom(key))
                {
                    files.Add(((RelativePath)item).RemoveWorkingFolder());
                }
            }
            return files;
        }
    }
}
