// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class CompilePhaseHandlerWithIncremental : IPhaseHandler
    {
        private CompilePhaseHandler _inner;

        public string Name => nameof(CompilePhaseHandlerWithIncremental);

        public BuildPhase Phase => BuildPhase.Compile;

        public DocumentBuildContext Context { get; }

        public IncrementalBuildContext IncrementalContext { get; }

        public BuildVersionInfo LastBuildVersionInfo { get; }

        public BuildVersionInfo CurrentBuildVersionInfo { get; }

        public BuildMessageInfo LastBuildMessageInfo { get; }

        public BuildMessageInfo CurrentBuildMessageInfo { get; }

        public CompilePhaseHandlerWithIncremental(CompilePhaseHandler inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }
            _inner = inner;
            Context = _inner.Context;
            IncrementalContext = Context.IncrementalBuildContext;
            LastBuildVersionInfo = IncrementalContext.LastBuildVersionInfo;
            LastBuildMessageInfo = BuildPhaseUtility.GetPhaseMessageInfo(LastBuildVersionInfo?.BuildMessage, Phase);
            CurrentBuildVersionInfo = IncrementalContext.CurrentBuildVersionInfo;
            CurrentBuildMessageInfo = BuildPhaseUtility.GetPhaseMessageInfo(CurrentBuildVersionInfo.BuildMessage, Phase);
        }

        public void Handle(List<HostService> hostServices, int maxParallelism)
        {
            PreHandle(hostServices);
            _inner.Handle(hostServices, maxParallelism);
            PostHandle(hostServices);
        }

        #region Private Methods

        private void PreHandle(List<HostService> hostServices)
        {
            foreach (var hostService in hostServices)
            {
                hostService.DependencyGraph = CurrentBuildVersionInfo.Dependency;
                using (new LoggerPhaseScope("RegisterDependencyTypeFromProcessor", LogLevel.Verbose))
                {
                    hostService.RegisterDependencyType();
                }
            }
            ReloadDependency(hostServices);
            Logger.RegisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void PostHandle(List<HostService> hostServices)
        {
            ReportReference(hostServices);
            ReportDependency(hostServices);
            CurrentBuildVersionInfo.Dependency.ResolveReference();
            foreach (var h in hostServices.Where(h => h.CanIncrementalBuild))
            {
                foreach (var file in GetFilesToRelayMessages(h))
                {
                    LastBuildMessageInfo.Replay(file);
                }
            }
            foreach (var h in hostServices.Where(h => h.ShouldTraceIncrementalInfo))
            {
                h.SaveIntermediateModel(IncrementalContext);
            }
            IncrementalContext.UpdateBuildVersionInfoPerDependencyGraph();
            Logger.UnregisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void ReportReference(List<HostService> hostServices)
        {
            foreach (var h in hostServices)
            {
                foreach (var model in h.Models)
                {
                    foreach (var u in model.Uids.Select(u => u.Name))
                    {
                        h.ReportReference(model, u, DependencyItemSourceType.Uid);
                    }
                }
            }
        }

        private void ReloadDependency(IEnumerable<HostService> hostServices)
        {
            // restore dependency graph from last dependency graph for unchanged files
            using (new LoggerPhaseScope("ReportDependencyFromLastBuild", LogLevel.Diagnostic))
            {
                var ldg = LastBuildVersionInfo?.Dependency;
                if (ldg != null)
                {
                    CurrentBuildVersionInfo.Dependency.ReportDependency(from r in ldg.ReportedBys
                                                                        where !IncrementalContext.ChangeDict.ContainsKey(r) || IncrementalContext.ChangeDict[r] == ChangeKindWithDependency.None
                                                                        from i in ldg.GetDependencyReportedBy(r)
                                                                        select i);
                }
            }
        }

        private IEnumerable<string> GetFilesToRelayMessages(HostService hs)
        {
            var files = new HashSet<string>();
            foreach (var f in hs.GetUnloadedModelFiles(IncrementalContext))
            {
                files.Add(f);

                // warnings from token file won't be delegated to article, so we need to add it manually
                var key = ((RelativePath)f).GetPathFromWorkingFolder();
                foreach (var item in CurrentBuildVersionInfo.Dependency.GetAllDependencyFrom(key))
                {
                    if (item.Type == DependencyTypeName.Include)
                    {
                        files.Add(((RelativePath)item.To.Value).RemoveWorkingFolder());
                    }
                }
            }
            return files;
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
                    var dps = GetUidDependency(m).Distinct();
                    CurrentBuildVersionInfo.Dependency.ReportDependency(dps);
                }
            }
        }

        private void ReportFileDependency(IEnumerable<HostService> hostServices)
        {
            foreach (var hostService in hostServices)
            {
                foreach (var m in hostService.Models)
                {
                    if (m.LinkToFiles.Count != 0)
                    {
                        var dps = GetFileDependency(m).Distinct();
                        CurrentBuildVersionInfo.Dependency.ReportDependency(dps);
                    }
                }
            }
        }

        private IEnumerable<DependencyItem> GetFileDependency(FileModel model)
        {
            string fromNode = ((RelativePath)model.OriginalFileAndType.File).GetPathFromWorkingFolder().ToString();
            foreach (var f in model.LinkToFiles)
            {
                ImmutableList<LinkSourceInfo> list;
                if (model.FileLinkSources.TryGetValue(f, out list))
                {
                    foreach (var fileLinkSourceFile in list)
                    {
                        var sourceFile = fileLinkSourceFile.SourceFile != null ? ((RelativePath)fileLinkSourceFile.SourceFile).GetPathFromWorkingFolder().ToString() : fromNode;
                        if (!string.IsNullOrEmpty(fileLinkSourceFile.Anchor))
                        {
                            yield return new DependencyItem(sourceFile, f, sourceFile, DependencyTypeName.Bookmark);
                        }
                        else
                        {
                            yield return new DependencyItem(sourceFile, f, sourceFile, DependencyTypeName.File);
                        }
                    }
                }
                else
                {
                    yield return new DependencyItem(fromNode, f, fromNode, DependencyTypeName.File);
                }
            }
        }

        private IEnumerable<DependencyItem> GetUidDependency(FileModel model)
        {
            var items = GetUidDependencyCore(model);
            if (model.Type == DocumentType.Overwrite)
            {
                items = items.Concat(GetUidDependencyForOverwrite(model));
            }
            return items;
        }

        private IEnumerable<DependencyItem> GetUidDependencyForOverwrite(FileModel model)
        {
            if (model.Type != DocumentType.Overwrite)
            {
                yield break;
            }
            string fromNode = ((RelativePath)model.OriginalFileAndType.File).GetPathFromWorkingFolder().ToString();
            var uids = model.Uids.Select(u => u.File).ToImmutableHashSet();
            foreach (var uid in uids)
            {
                var item = new DependencyItemSourceInfo(DependencyItemSourceType.Uid, uid);
                yield return new DependencyItem(fromNode, item, fromNode, DependencyTypeName.Uid);
                yield return new DependencyItem(item, fromNode, fromNode, DependencyTypeName.Uid);
            }
        }

        private IEnumerable<DependencyItem> GetUidDependencyCore(FileModel model)
        {
            string fromNode = ((RelativePath)model.OriginalFileAndType.File).GetPathFromWorkingFolder().ToString();
            foreach (var uid in model.LinkToUids)
            {
                var item = new DependencyItemSourceInfo(DependencyItemSourceType.Uid, uid);
                ImmutableList<LinkSourceInfo> list;
                if (model.UidLinkSources.TryGetValue(uid, out list))
                {
                    foreach (var uidLinkSourceFile in list)
                    {
                        var sourceFile = uidLinkSourceFile.SourceFile != null ? ((RelativePath)uidLinkSourceFile.SourceFile).GetPathFromWorkingFolder().ToString() : fromNode;
                        if (!string.IsNullOrEmpty(uidLinkSourceFile.Anchor))
                        {
                            yield return new DependencyItem(sourceFile, item, sourceFile, DependencyTypeName.Bookmark);
                        }
                        else
                        {
                            yield return new DependencyItem(sourceFile, item, sourceFile, DependencyTypeName.Uid);
                        }
                    }
                }
                else
                {
                    yield return new DependencyItem(fromNode, item, fromNode, DependencyTypeName.Uid);
                }
            }
        }

        #endregion
    }
}
