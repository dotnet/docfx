// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class LinkPhaseHandlerWithIncremental : IPhaseHandler
    {
        private LinkPhaseHandler _inner;

        public string Name => nameof(LinkPhaseHandlerWithIncremental);

        public BuildPhase Phase => BuildPhase.Link;

        public DocumentBuildContext Context { get; }

        public TemplateProcessor TemplateProcessor { get; }

        public IncrementalBuildContext IncrementalContext { get; }

        public BuildVersionInfo LastBuildVersionInfo { get; }

        public BuildVersionInfo CurrentBuildVersionInfo { get; }

        public BuildMessageInfo LastBuildMessageInfo { get; }

        public BuildMessageInfo CurrentBuildMessageInfo { get; }

        public LinkPhaseHandlerWithIncremental(LinkPhaseHandler inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }
            _inner = inner;
            Context = _inner.Context;
            TemplateProcessor = _inner.TemplateProcessor;
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
            ReloadModelsPerChanges(hostServices);
            RegisterUnloadedXRefSpec(hostServices);
            RegisterUnloadedFileMap(hostServices);
            Logger.RegisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void PostHandle(List<HostService> hostServices)
        {
            ProcessUnloadedTemplateDependency(hostServices);
            UpdateManifest();
            UpdateFileMap(hostServices);
            UpdateXrefMap(hostServices);
            RelayBuildMessage(hostServices);
            Logger.UnregisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void ReloadModels(IEnumerable<HostService> hostServices)
        {
            foreach (var hostService in hostServices.Where(h => h.CanIncrementalBuild))
            {
                hostService.ReloadUnloadedModels(IncrementalContext, Phase);
            }
        }

        private void ReloadModelsPerChanges(IEnumerable<HostService> hostServices)
        {
            var newChanges = IncrementalContext.ExpandDependency(CurrentBuildVersionInfo.Dependency, d => CurrentBuildVersionInfo.Dependency.DependencyTypes[d.Type].Phase == Phase);
            foreach (var hostService in hostServices.Where(h => h.CanIncrementalBuild))
            {
                hostService.ReloadModelsPerIncrementalChanges(IncrementalContext, newChanges, Phase);
            }
        }

        private void RegisterUnloadedXRefSpec(IEnumerable<HostService> hostServices)
        {
            var lastXrefMap = LastBuildVersionInfo?.XRefSpecMap;
            foreach (var h in hostServices.Where(h => h.CanIncrementalBuild))
            {
                foreach (var file in h.GetUnloadedModelFiles(IncrementalContext))
                {
                    if (lastXrefMap == null)
                    {
                        throw new BuildCacheException($"Full build hasn't loaded XRefMap.");
                    }
                    IEnumerable<XRefSpec> specs;
                    if (!lastXrefMap.TryGetValue(file, out specs))
                    {
                        throw new BuildCacheException($"Last build hasn't loaded xrefspec for file: ({file}).");
                    }
                    CurrentBuildVersionInfo.XRefSpecMap[file] = specs;
                    foreach (var spec in specs)
                    {
                        Context.RegisterInternalXrefSpec(spec);
                    }
                }
            }
        }

        private void RegisterUnloadedFileMap(IEnumerable<HostService> hostServices)
        {
            var lastFileMap = LastBuildVersionInfo?.FileMap;
            foreach (var h in hostServices.Where(h => h.CanIncrementalBuild))
            {
                foreach (var file in h.GetUnloadedModelFiles(IncrementalContext))
                {
                    var fileFromWorkingFolder = ((RelativePath)file).GetPathFromWorkingFolder();
                    if (lastFileMap == null)
                    {
                        throw new BuildCacheException($"Full build hasn't loaded File Map.");
                    }
                    string path;

                    // for overwrite files, it don't exist in filemap
                    if (lastFileMap.TryGetValue(fileFromWorkingFolder, out path))
                    {
                        Context.SetFilePath(fileFromWorkingFolder, path);
                    }
                }
            }
        }

        private void ProcessUnloadedTemplateDependency(IEnumerable<HostService> hostServices)
        {
            var loaded = Context.ManifestItems;
            var unloaded = GetUnloadedManifestItems(hostServices);
            var types = new HashSet<string>(unloaded.Select(m => m.DocumentType).Except(loaded.Select(m => m.DocumentType)));
            if (types.Count > 0)
            {
                TemplateProcessor.ProcessDependencies(types, Context.ApplyTemplateSettings);
            }
            foreach (var m in unloaded)
            {
                Context.ManifestItems.Add(m);
            }
        }

        private void UpdateManifest()
        {
            Context.ManifestItems.Shrink(IncrementalContext.BaseDir);
            CurrentBuildVersionInfo.Manifest = Context.ManifestItems;
            CurrentBuildVersionInfo.SaveManifest(IncrementalContext.BaseDir);
        }

        private void UpdateFileMap(IEnumerable<HostService> hostServices)
        {
            CurrentBuildVersionInfo.FileMap = Context.FileMap;
        }

        private void UpdateXrefMap(IEnumerable<HostService> hostServices)
        {
            var map = CurrentBuildVersionInfo.XRefSpecMap;
            foreach (var h in hostServices)
            {
                foreach (var f in h.Models)
                {
                    if (f.Type == DocumentType.Overwrite)
                    {
                        map[f.OriginalFileAndType.File] = Enumerable.Empty<XRefSpec>();
                    }
                    else
                    {
                        map[f.OriginalFileAndType.File] = (from uid in f.Uids
                                                           let s = Context.GetXrefSpec(uid.Name)
                                                           where s != null
                                                           select s).ToList();
                    }
                }
            }
        }

        private void RelayBuildMessage(IEnumerable<HostService> hostServices)
        {
            foreach (var h in hostServices.Where(h => h.CanIncrementalBuild))
            {
                foreach (var file in GetFilesToRelayMessages(h))
                {
                    LastBuildMessageInfo.Replay(file);
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

        private List<ManifestItem> GetUnloadedManifestItems(IEnumerable<HostService> hostServices)
        {
            if (LastBuildVersionInfo == null)
            {
                return new List<ManifestItem>();
            }
            return (from h in hostServices
                    where h.CanIncrementalBuild
                    from f in h.GetUnloadedModelFiles(IncrementalContext)
                    from mani in LastBuildVersionInfo.Manifest
                    where FilePathComparer.OSPlatformSensitiveStringComparer.Equals(f, mani.SourceRelativePath)
                    let copied = mani.Clone(isIncremental: true, sourceRelativePath: f)
                    select copied).ToList();
        }

        #endregion
    }
}
