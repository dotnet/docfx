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

    internal class PostbuildPhaseHandlerWithIncremental : IPhaseHandler
    {
        private PostbuildPhaseHandler _inner;
        private List<string> _unloadedFiles;

        public DocumentBuildContext Context { get; }

        public TemplateProcessor TemplateProcessor { get; }

        public IncrementalBuildContext IncrementalContext { get; }

        public BuildVersionInfo LastBuildVersionInfo { get; }

        public BuildVersionInfo CurrentBuildVersionInfo { get; }

        public BuildMessageInfo LastBuildMessageInfo { get; }

        public BuildMessageInfo CurrentBuildMessageInfo { get; }

        public PostbuildPhaseHandlerWithIncremental(PostbuildPhaseHandler inner)
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

        private void PreHandle(List<HostService> hostServices)
        {
            ReloadModels(hostServices);
            _unloadedFiles = GetUnloaded().ToList();
            RegisterUnloadedXRefSpec();
            Logger.RegisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void PostHandle(List<HostService> hostServices)
        {
            ProcessUnloadedTemplateDependency();
            UpdateManifest();
            UpdateFileMap();
            UpdateXrefMap(hostServices);
            SaveOutputs(hostServices);
            RelayBuildMessage();
            Logger.UnregisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void ReloadModels(IEnumerable<HostService> hostServices)
        {
            foreach (var hostService in hostServices.Where(h => h.CanIncrementalBuild))
            {
                hostService.ReloadUnloadedModels(IncrementalContext, BuildPhase.PostBuild);
            }
        }

        private void ReloadModelsPerChanges(IEnumerable<HostService> hostServices)
        {
            var newChanges = IncrementalContext.ExpandDependency(d => CurrentBuildVersionInfo.Dependency.DependencyTypes[d.Type].Phase == BuildPhase.PostBuild);
            foreach (var hostService in hostServices.Where(h => h.CanIncrementalBuild))
            {
                hostService.ReloadModelsPerIncrementalChanges(IncrementalContext, newChanges, BuildPhase.PostBuild);
            }
        }

        private void RegisterUnloadedXRefSpec()
        {
            var lastXrefMap = LastBuildVersionInfo?.XRefSpecMap;
            foreach (var m in _unloadedFiles)
            {
                if (lastXrefMap == null)
                {
                    throw new BuildCacheException($"Full build hasn't loaded XRefMap.");
                }
                IEnumerable<XRefSpec> specs;
                if (!lastXrefMap.TryGetValue(m, out specs))
                {
                    throw new BuildCacheException($"Last build hasn't loaded xrefspec for file: ({m}).");
                }
                CurrentBuildVersionInfo.XRefSpecMap[m] = specs;
                foreach (var spec in specs)
                {
                    Context.RegisterInternalXrefSpec(spec);
                }
            }
        }

        private void ProcessUnloadedTemplateDependency()
        {
            var loaded = Context.ManifestItems;
            var unloaded = GetUnloadedManifestItems();
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
            CurrentBuildVersionInfo.Manifest = Context.ManifestItems;
        }

        private void UpdateFileMap()
        {
            var lastFileMap = LastBuildVersionInfo?.FileMap;
            foreach (var file in _unloadedFiles)
            {
                var fileFromWorkingFolder = ((RelativePath)file).GetPathFromWorkingFolder();
                if (lastFileMap == null)
                {
                    throw new BuildCacheException($"Full build hasn't loaded File Map.");
                }
                string path;
                if (!lastFileMap.TryGetValue(fileFromWorkingFolder, out path))
                {
                    throw new BuildCacheException($"Last build hasn't loaded file map item for file: {file}.");
                }
                Context.FileMap[fileFromWorkingFolder] = path;
            }
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

        private void SaveOutputs(IEnumerable<HostService> hostServices)
        {
            var outputDir = Context.BuildOutputFolder;
            var lo = LastBuildVersionInfo?.BuildOutputs;

            foreach (var item in from m in Context.ManifestItems
                                 from output in m.OutputFiles.Values
                                 select new
                                 {
                                     Path = Path.Combine(outputDir, output.RelativePath),
                                     SourcePath = m.SourceRelativePath,
                                 })
            {
                IncrementalUtility.RetryIO(() =>
                {
                    string fileName = IncrementalUtility.GetRandomEntry(IncrementalContext.BaseDir);
                    if (_unloadedFiles.Contains(item.SourcePath))
                    {
                        if (lo == null)
                        {
                            throw new BuildCacheException($"Full build hasn't loaded build outputs.");
                        }
                        string lfn;
                        if (!lo.TryGetValue(item.Path, out lfn))
                        {
                            throw new BuildCacheException($"Last build hasn't loaded output: {item.Path}.");
                        }
                        File.Copy(Path.Combine(IncrementalContext.LastBaseDir, lfn), Path.Combine(IncrementalContext.BaseDir, fileName));
                        Directory.CreateDirectory(Path.GetDirectoryName(item.Path));
                        File.Copy(Path.Combine(IncrementalContext.BaseDir, fileName), item.Path, true);
                    }
                    else
                    {
                        var hs = hostServices.Single(h => IncrementalContext.GetModelLoadInfo(h).ContainsKey(item.SourcePath));
                        if (hs.ShouldTraceIncrementalInfo)
                        {
                            File.Copy(item.Path, Path.Combine(IncrementalContext.BaseDir, fileName));
                        }
                    }
                    CurrentBuildVersionInfo.BuildOutputs.Add(item.Path, fileName);
                });
            }
        }

        private void RelayBuildMessage()
        {
            foreach (var file in _unloadedFiles)
            {
                LastBuildMessageInfo.Replay(file);
            }
        }

        private List<ManifestItem> GetUnloadedManifestItems()
        {
            if (LastBuildVersionInfo == null)
            {
                return new List<ManifestItem>();
            }
            return (from f in _unloadedFiles
                    from mani in LastBuildVersionInfo.Manifest
                    where f == mani.SourceRelativePath
                    select mani).ToList();
        }

        private IEnumerable<string> GetUnloaded()
        {
            return from d in IncrementalContext.ModelLoadInfo.Values
                   from m in d
                   where m.Value == null
                   select m.Key;
        }

        private static BuildMessageInfo GetPhaseMessageInfo(BuildMessage messages)
        {
            if (messages == null)
            {
                return null;
            }

            BuildMessageInfo message;
            if (!messages.TryGetValue(BuildPhase.PostBuild, out message))
            {
                messages[BuildPhase.PostBuild] = message = new BuildMessageInfo();
            }
            return message;
        }

        #endregion
    }
}
