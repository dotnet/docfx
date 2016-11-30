// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Plugins;

    internal class PrebuildBuildPhaseHandlerWithIncremental : IPhaseHandler
    {
        private PrebuildBuildPhaseHandler _inner;

        private DocumentBuildContext Context;

        public IncrementalBuildContext IncrementalContext { get; }

        public BuildVersionInfo LastBuildVersionInfo { get; }

        public BuildVersionInfo CurrentBuildVersionInfo { get; }

        public BuildMessageInfo LastBuildMessageInfo { get; }

        public BuildMessageInfo CurrentBuildMessageInfo { get; }

        public PrebuildBuildPhaseHandlerWithIncremental(PrebuildBuildPhaseHandler inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }
            _inner = inner;
            Context = _inner.Context;
            IncrementalContext = Context.IncrementalBuildContext;
            LastBuildVersionInfo = IncrementalContext.LastBuildVersionInfo;
            LastBuildMessageInfo = GetPhaseMessageInfo(LastBuildVersionInfo?.BuildMessage);
            CurrentBuildVersionInfo = IncrementalContext.CurrentBuildVersionInfo;
            CurrentBuildMessageInfo = GetPhaseMessageInfo(CurrentBuildVersionInfo.BuildMessage);
        }

        public void Handle(List<HostService> hostServices, int maxParallelism)
        {
            PreHandle(hostServices);
            _inner.Handle(hostServices, maxParallelism);
            PostHandle(hostServices);
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

        private void PreHandle(List<HostService> hostServices)
        {
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

        private void PostHandle(List<HostService> hostServices)
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
            ReportDependency(hostServices);
            Logger.UnregisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void ReportDependency(IEnumerable<HostService> hostServices)
        {
            ReportFileDependency(hostServices);
            ReportUidDependency(hostServices);
        }

        private void ReportUidDependency(IEnumerable<HostService> hostServices)
        {
            foreach (var hostService in hostServices)
            {
                foreach (var m in hostService.Models)
                {
                    var dps = GetUidDependency(m).ToList();
                    if (dps.Count != 0)
                    {
                        CurrentBuildVersionInfo.Dependency.ReportDependency(dps);
                    }
                }
            }
        }

        private void ReportFileDependency(IEnumerable<HostService> hostServices)
        {
            foreach (var hostService in hostServices)
            {
                foreach (var m in hostService.Models)
                {
                    if (m.Type == DocumentType.Overwrite)
                    {
                        continue;
                    }
                    if (m.LinkToFiles.Count != 0)
                    {
                        string fromNode = ((RelativePath)m.OriginalFileAndType.File).GetPathFromWorkingFolder().ToString();
                        var dps = from f in m.LinkToFiles
                                  select new DependencyItem(fromNode, f, fromNode, DependencyTypeName.File);
                        CurrentBuildVersionInfo.Dependency.ReportDependency(dps);
                    }
                }
            }
        }

        private IEnumerable<DependencyItem> GetUidDependency(FileModel model)
        {
            var uids = model.Type == DocumentType.Overwrite ? model.Uids.Select(u => u.File).ToImmutableHashSet() : model.LinkToUids;
            if (uids.Count == 0)
            {
                yield break;
            }
            string fromNode = ((RelativePath)model.OriginalFileAndType.File).GetPathFromWorkingFolder().ToString();
            foreach (var f in GetFilesFromUids(uids))
            {
                yield return new DependencyItem(fromNode, f, fromNode, DependencyTypeName.Uid);
                if (model.Type == DocumentType.Overwrite)
                {
                    yield return new DependencyItem(f, fromNode, fromNode, DependencyTypeName.Uid);
                }
            }
        }

        private IEnumerable<string> GetFilesFromUids(IEnumerable<string> uids)
        {
            foreach (var uid in uids)
            {
                if (string.IsNullOrEmpty(uid))
                {
                    continue;
                }
                XRefSpec spec;
                if (Context.XRefSpecMap.TryGetValue(uid, out spec) && spec.Href != null)
                {
                    yield return spec.Href;
                }
            }
        }

        #endregion
    }
}
