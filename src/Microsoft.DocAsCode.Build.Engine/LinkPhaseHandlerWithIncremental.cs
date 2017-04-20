// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

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
            _inner.PostbuildAndSave(hostServices, maxParallelism);
            using (new LoggerPhaseScope("SaveExternalXRefSpec", LogLevel.Verbose))
            {
                SaveExternalXRefSpec();
            }
            _inner.ProcessManifest(hostServices, maxParallelism);
            PostHandle(hostServices);
        }

        #region Private Methods

        private void PreHandle(List<HostService> hostServices)
        {
            using (new LoggerPhaseScope("ReloadModelsPerChanges", LogLevel.Verbose))
            {
                ReloadModelsPerChanges(hostServices);
            }

            using (new LoggerPhaseScope("RegisterUnloadedXRefSpec", LogLevel.Verbose))
            {
                RegisterUnloadedXRefSpec(hostServices);
            }

            using (new LoggerPhaseScope("RegisterUnloadedFileMap", LogLevel.Verbose))
            {
                RegisterUnloadedFileMap(hostServices);
            }

            using (new LoggerPhaseScope("LoadExternalXRefSpec", LogLevel.Verbose))
            {
                LoadExternalXRefSpec();
            }

            Logger.RegisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void PostHandle(List<HostService> hostServices)
        {
            using (new LoggerPhaseScope("ProcessUnloadedTemplateDependency", LogLevel.Verbose))
            {
                ProcessUnloadedTemplateDependency(hostServices);
            }

            using (new LoggerPhaseScope("UpdateManifest", LogLevel.Verbose))
            {
                UpdateManifest();
            }

            using (new LoggerPhaseScope("UpdateFileMap", LogLevel.Verbose))
            {
                UpdateFileMap(hostServices);
            }

            using (new LoggerPhaseScope("UpdateXrefMap", LogLevel.Verbose))
            {
                UpdateXrefMap(hostServices);
            }

            using (new LoggerPhaseScope("SaveContextInfo", LogLevel.Verbose))
            {
                SaveContextInfo(hostServices);
            }

            using (new LoggerPhaseScope("RelayBuildMessage", LogLevel.Verbose))
            {
                BuildPhaseUtility.RelayBuildMessage(IncrementalContext, hostServices, Phase);
            }

            Logger.UnregisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void SaveContextInfo(List<HostService> hostServices)
        {
            foreach (var h in hostServices)
            {
                IncrementalContext.SaveContextInfo(h);
            }
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
            var newChanges = new HashSet<string>(
                IncrementalContext.ExpandDependency(LastBuildVersionInfo?.Dependency, d => CurrentBuildVersionInfo.Dependency.DependencyTypes[d.Type].Phase == Phase)
                .Concat(
                    IncrementalContext.ExpandDependency(CurrentBuildVersionInfo.Dependency, d => CurrentBuildVersionInfo.Dependency.DependencyTypes[d.Type].Phase == Phase)));
            foreach (var hostService in hostServices.Where(h => h.CanIncrementalBuild))
            {
                hostService.ReloadModelsPerIncrementalChanges(IncrementalContext, newChanges, Phase);
                hostService.IncrementalInfos = IncrementalContext.GetModelIncrementalInfo(hostService, Phase);
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
                    List<XRefSpec> specs;
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
                    FileMapItem item;

                    // for overwrite files, it don't exist in filemap
                    if (lastFileMap.TryGetValue(file, out item))
                    {
                        foreach (var pair in item)
                        {
                            Context.SetFilePath(pair.Key, pair.Value);
                        }
                        CurrentBuildVersionInfo.FileMap[file] = item;
                    }
                }
            }
        }

        private void ProcessUnloadedTemplateDependency(IEnumerable<HostService> hostServices)
        {
            var loaded = Context.ManifestItems;
            IEnumerable<ManifestItem> unloaded;
            using (new LoggerPhaseScope("GetUnloadedManifestItems", LogLevel.Verbose))
            {
                unloaded = GetUnloadedManifestItems(hostServices);
            }
            var types = new HashSet<string>(unloaded.Select(m => m.DocumentType).Except(loaded.Select(m => m.DocumentType)));
            if (types.Count > 0)
            {
                using (new LoggerPhaseScope("ProcessDependencies", LogLevel.Verbose))
                {
                    TemplateProcessor.ProcessDependencies(types, Context.ApplyTemplateSettings);
                }
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
            CurrentBuildVersionInfo.SaveManifest();
        }

        private void UpdateFileMap(IEnumerable<HostService> hostServices)
        {
            var map = CurrentBuildVersionInfo.FileMap;
            foreach (var h in hostServices)
            {
                foreach (var f in h.Models)
                {
                    var path = Context.GetFilePath(f.Key);
                    if (path != null)
                    {
                        FileMapItem item;
                        if (!map.TryGetValue(f.OriginalFileAndType.File, out item))
                        {
                            map[f.OriginalFileAndType.File] = item = new FileMapItem();
                        }
                        item[f.Key] = path;
                    }
                }
            }
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
                        map[f.OriginalFileAndType.File] = new List<XRefSpec>();
                    }
                    else
                    {
                        List<XRefSpec> specs;
                        if (!map.TryGetValue(f.OriginalFileAndType.File, out specs))
                        {
                            map[f.OriginalFileAndType.File] = specs = new List<XRefSpec>();
                        }
                        specs.AddRange(from uid in f.Uids
                                       let s = Context.GetXrefSpec(uid.Name)
                                       where s != null
                                       select s);
                    }
                }
            }
        }

        private List<ManifestItem> GetUnloadedManifestItems(IEnumerable<HostService> hostServices)
        {
            if (LastBuildVersionInfo == null)
            {
                return new List<ManifestItem>();
            }
            var unloadedFiles = (from h in hostServices
                                 where h.CanIncrementalBuild
                                 from f in h.GetUnloadedModelFiles(IncrementalContext)
                                 select f).ToDictionary(f => f, f => f, FilePathComparer.OSPlatformSensitiveStringComparer);

            using (new LoggerPhaseScope("UpdateItems", LogLevel.Verbose))
            {
                return (from mani in LastBuildVersionInfo.Manifest
                        where unloadedFiles.ContainsKey(mani.SourceRelativePath)
                        let copied = UpdateItem(mani, unloadedFiles[mani.SourceRelativePath])
                        select copied).ToList();
            }
        }

        private ManifestItem UpdateItem(ManifestItem item, string sourceRelativePath)
        {
            var result = item.Clone();
            result.IsIncremental = true;
            result.SourceRelativePath = sourceRelativePath;
            Parallel.ForEach(
                from ofi in result.OutputFiles.Values
                where ofi.LinkToPath != null
                where ofi.LinkToPath.Length > IncrementalContext.LastBaseDir.Length
                where ofi.LinkToPath.StartsWith(IncrementalContext.LastBaseDir)
                where (ofi.LinkToPath[IncrementalContext.LastBaseDir.Length] == '\\' || ofi.LinkToPath[IncrementalContext.LastBaseDir.Length] == '/')
                select ofi,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                ofi =>
                {
                    IncrementalUtility.RetryIO(() =>
                    {
                        var path = Path.Combine(IncrementalContext.BaseDir, IncrementalUtility.GetRandomEntry(IncrementalContext.BaseDir));
                        File.Copy(Environment.ExpandEnvironmentVariables(ofi.LinkToPath), Environment.ExpandEnvironmentVariables(path));
                        ofi.LinkToPath = path;
                    });
                });

            return result;
        }

        private void LoadExternalXRefSpec()
        {
            if (LastBuildVersionInfo?.ExternalXRefSpecFile != null)
            {
                using (var reader = File.OpenText(Path.Combine(LastBuildVersionInfo.BaseDir, LastBuildVersionInfo.ExternalXRefSpecFile)))
                {
                    Context.LoadExternalXRefSpec(reader);
                }
            }
        }

        private void SaveExternalXRefSpec()
        {
            CurrentBuildVersionInfo.ExternalXRefSpecFile = IncrementalUtility.CreateRandomFileName(CurrentBuildVersionInfo.BaseDir);
            using (var writer = File.CreateText(Path.Combine(CurrentBuildVersionInfo.BaseDir, CurrentBuildVersionInfo.ExternalXRefSpecFile)))
            {
                Context.SaveExternalXRefSpec(writer);
            }
        }

        #endregion
    }
}
