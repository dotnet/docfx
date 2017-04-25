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
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
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

                if (hostService.ShouldTraceIncrementalInfo)
                {
                    hostService.IncrementalInfos = IncrementalContext.GetModelIncrementalInfo(hostService, Phase);
                }
            }
            var fileSet = new HashSet<string>(from h in hostServices
                                              where !h.CanIncrementalBuild
                                              from f in h.Models
                                              select IncrementalUtility.GetDependencyKey(f.OriginalFileAndType),
                                                  FilePathComparer.OSPlatformSensitiveStringComparer);
            ReloadDependency(fileSet);
            LoadContextInfo(hostServices);
            RegisterUnloadedTocRestructions(fileSet);
            Logger.RegisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void PostHandle(List<HostService> hostServices)
        {
            ReportReference(hostServices);
            ReportDependency(hostServices);
            UpdateTocRestructions(hostServices);
            CurrentBuildVersionInfo.Dependency.ResolveReference();
            foreach (var h in hostServices.Where(h => h.ShouldTraceIncrementalInfo))
            {
                h.SaveIntermediateModel(IncrementalContext);
            }
            IncrementalContext.UpdateBuildVersionInfoPerDependencyGraph();
            BuildPhaseUtility.RelayBuildMessage(IncrementalContext, hostServices, Phase);
            Logger.UnregisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void LoadContextInfo(List<HostService> hostServices)
        {
            using (new LoggerPhaseScope("LoadPluginContextInfo", LogLevel.Verbose))
            {
                foreach (var h in hostServices)
                {
                    IncrementalContext.LoadContextInfo(h);
                }
            }
        }

        private void ReportReference(List<HostService> hostServices)
        {
            foreach (var h in hostServices)
            {
                foreach (var model in h.Models)
                {
                    // to-do: move to plugins.
                    if (model.Type == DocumentType.Overwrite)
                    {
                        continue;
                    }
                    foreach (var u in model.Uids.Select(u => u.Name))
                    {
                        h.ReportReference(model, u, DependencyItemSourceType.Uid);
                    }
                }
            }
        }

        private void ReloadDependency(HashSet<string> nonIncreSet)
        {
            // restore dependency graph from last dependency graph for unchanged files
            using (new LoggerPhaseScope("ReportDependencyFromLastBuild", LogLevel.Verbose))
            {
                var ldg = LastBuildVersionInfo?.Dependency;
                if (ldg != null)
                {
                    CurrentBuildVersionInfo.Dependency.ReportReference(from r in ldg.ReferenceReportedBys
                                                                       where !IncrementalContext.ChangeDict.ContainsKey(r) || IncrementalContext.ChangeDict[r] == ChangeKindWithDependency.None
                                                                       where !nonIncreSet.Contains(r)
                                                                       from reference in ldg.GetReferenceReportedBy(r)
                                                                       select reference);
                    CurrentBuildVersionInfo.Dependency.ReportDependency(from r in ldg.ReportedBys
                                                                        where !IncrementalContext.ChangeDict.ContainsKey(r) || IncrementalContext.ChangeDict[r] == ChangeKindWithDependency.None
                                                                        where !nonIncreSet.Contains(r)
                                                                        from i in ldg.GetDependencyReportedBy(r)
                                                                        select i);
                }
            }
        }

        // TO-DO: move to plugins
        private void RegisterUnloadedTocRestructions(HashSet<string> nonIncreSet)
        {
            using (new LoggerPhaseScope("RegisterUnloadedTocRestructionsFromLastBuild", LogLevel.Verbose))
            {
                var restructions = LastBuildVersionInfo?.TocRestructions;
                if (restructions == null)
                {
                    return;
                }
                foreach (var pair in restructions)
                {
                    var pathFromWorkingFolder = ((RelativePath)pair.Key).GetPathFromWorkingFolder();
                    if (nonIncreSet.Contains(pathFromWorkingFolder))
                    {
                        continue;
                    }
                    if (!IncrementalContext.ChangeDict.ContainsKey(pathFromWorkingFolder) || IncrementalContext.ChangeDict[pathFromWorkingFolder] == ChangeKindWithDependency.None)
                    {
                        _inner.Restructions.AddRange(pair.Value);
                    }
                }
            }
        }

        private void UpdateTocRestructions(IEnumerable<HostService> hostServices)
        {
            var dict = (from r in _inner.Restructions
                        from srcFile in r.SourceFiles ?? Enumerable.Empty<FileAndType>()
                        select new
                        {
                            File = srcFile.File,
                            Item = r,
                        } into item
                        group item by item.File).ToDictionary(p => p.Key, p => p.Select(i => i.Item).ToList());
            var restructions = CurrentBuildVersionInfo.TocRestructions;
            foreach (var pair in dict)
            {
                restructions[pair.Key] = pair.Value;
            }
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
                if (model.FileLinkSources.TryGetValue(f, out ImmutableList<LinkSourceInfo> list))
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
                // to-do: move to plugins.
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
            var uids = model.Uids.Select(u => u.Name).ToImmutableHashSet();
            foreach (var uid in uids)
            {
                var item = new DependencyItemSourceInfo(DependencyItemSourceType.Uid, uid);
                yield return new DependencyItem(fromNode, item, fromNode, DependencyTypeName.Overwrite);
                yield return new DependencyItem(item, fromNode, fromNode, DependencyTypeName.Overwrite);
            }
        }

        private IEnumerable<DependencyItem> GetUidDependencyCore(FileModel model)
        {
            string fromNode = ((RelativePath)model.OriginalFileAndType.File).GetPathFromWorkingFolder().ToString();
            foreach (var uid in model.LinkToUids)
            {
                var item = new DependencyItemSourceInfo(DependencyItemSourceType.Uid, uid);
                if (model.UidLinkSources.TryGetValue(uid, out ImmutableList<LinkSourceInfo> list))
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
