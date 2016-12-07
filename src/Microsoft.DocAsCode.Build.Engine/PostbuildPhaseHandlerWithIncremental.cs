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

        public string Name => GetType().Name;

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
            ReloadModelsPerChanges(hostServices);
            RegisterUnloadedXRefSpec(hostServices);
            Logger.RegisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void PostHandle(List<HostService> hostServices)
        {
            ProcessUnloadedTemplateDependency(hostServices);
            UpdateManifest();
            UpdateFileMap(hostServices);
            UpdateXrefMap(hostServices);
            SaveOutputs(hostServices);
            RelayBuildMessage(hostServices);
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
            CurrentBuildVersionInfo.Manifest = Context.ManifestItems;
        }

        private void UpdateFileMap(IEnumerable<HostService> hostServices)
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
                        Context.FileMap[fileFromWorkingFolder] = path;
                    }
                }
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
            var outputItems = (from m in Context.ManifestItems
                               from output in m.OutputFiles.Values
                               select new
                               {
                                   Path = output.RelativePath,
                                   SourcePath = m.SourceRelativePath,
                               } into items
                               group items by items.SourcePath).ToDictionary(g => g.Key, g => g.Select(p => p.Path).ToList());

            foreach (var h in hostServices.Where(h => h.ShouldTraceIncrementalInfo))
            {
                foreach (var pair in IncrementalContext.GetModelLoadInfo(h))
                {
                    List<string> items;
                    if (!outputItems.TryGetValue(pair.Key, out items))
                    {
                        continue;
                    }
                    foreach (var path in items)
                    {
                        string fileName = IncrementalUtility.GetRandomEntry(IncrementalContext.BaseDir);
                        string fullPath = Path.Combine(outputDir, path);
                        IncrementalUtility.RetryIO(() =>
                        {
                            if (pair.Value == null)
                            {
                                if (lo == null)
                                {
                                    throw new BuildCacheException($"Full build hasn't loaded build outputs.");
                                }
                                string lfn;
                                if (!lo.TryGetValue(path, out lfn))
                                {
                                    throw new BuildCacheException($"Last build hasn't loaded output: {path}.");
                                }

                                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                                File.Copy(Path.Combine(IncrementalContext.LastBaseDir, lfn), fullPath, true);
                            }

                            File.Copy(fullPath, Path.Combine(IncrementalContext.BaseDir, fileName));
                            CurrentBuildVersionInfo.BuildOutputs.Add(path, fileName);
                        });
                    }
                }
            }
        }

        private void RelayBuildMessage(IEnumerable<HostService> hostServices)
        {
            foreach (var h in hostServices.Where(h => h.CanIncrementalBuild))
            {
                foreach (var file in h.GetUnloadedModelFiles(IncrementalContext))
                {
                    LastBuildMessageInfo.Replay(file);
                }
            }
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
                    where f == mani.SourceRelativePath
                    select mani).ToList();
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
