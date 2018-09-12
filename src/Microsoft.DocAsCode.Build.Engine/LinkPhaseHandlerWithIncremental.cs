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
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
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
            _inner.ProcessManifest(hostServices, maxParallelism);
            using (new LoggerPhaseScope("SaveExternalXRefSpec", LogLevel.Verbose))
            {
                SaveExternalXRefSpec();
            }
            PostHandle(hostServices, maxParallelism);
        }

        #region Private Methods

        private void PreHandle(List<HostService> hostServices)
        {
            using (new LoggerPhaseScope("ReloadModelsPerChanges", LogLevel.Verbose))
            {
                if (IncrementalContext.IsTemplateUpdated)
                {
                    ReloadModels(hostServices);
                }
                else
                {
                    ReloadModelsPerChanges(hostServices);
                }
            }
            Parallel.Invoke(
                new Action(() =>
                {
                    using (new LoggerPhaseScope("RegisterUnloadedXRefSpec", LogLevel.Verbose))
                    {
                        RegisterUnloadedXRefSpec(hostServices);
                    }
                }),
                new Action(() =>
                {
                    using (new LoggerPhaseScope("RegisterUnloadedFileMap", LogLevel.Verbose))
                    {
                        RegisterUnloadedFileMap(hostServices);
                    }
                }),
                new Action(() =>
                {
                    using (new LoggerPhaseScope("LoadExternalXRefSpec", LogLevel.Verbose))
                    {
                        LoadExternalXRefSpec();
                    }
                }));
            Logger.RegisterListener(CurrentBuildMessageInfo.GetListener());
        }

        private void PostHandle(List<HostService> hostServices, int maxParallelism)
        {
            using (new LoggerPhaseScope("ProcessUnloadedTemplateDependency", LogLevel.Verbose))
            {
                ProcessUnloadedTemplateDependency(hostServices, maxParallelism);
            }

            using (new LoggerPhaseScope("UpdateManifest", LogLevel.Verbose))
            {
                UpdateManifest();
            }
            Parallel.Invoke(
                new Action(() =>
                {
                    using (new LoggerPhaseScope("UpdateFileMap", LogLevel.Verbose))
                    {
                        UpdateFileMap(hostServices);
                    }
                }),
                new Action(() =>
                {
                    using (new LoggerPhaseScope("UpdateXrefMap", LogLevel.Verbose))
                    {
                        UpdateXrefMap(hostServices);
                    }
                }),
                new Action(() =>
                {
                    using (new LoggerPhaseScope("SaveContextInfo", LogLevel.Verbose))
                    {
                        SaveContextInfo(hostServices);
                    }
                }));

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
                hostService.IncrementalInfos = IncrementalContext.GetModelIncrementalInfo(hostService, Phase);
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
                    if (!lastXrefMap.TryGetValue(file, out List<XRefSpec> specs))
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

                    // for overwrite files, it don't exist in filemap
                    if (lastFileMap.TryGetValue(file, out FileMapItem item))
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

        private void ProcessUnloadedTemplateDependency(IEnumerable<HostService> hostServices, int maxParallelism)
        {
            var loaded = Context.ManifestItems;
            IEnumerable<ManifestItem> unloaded;
            using (new LoggerPhaseScope("GetUnloadedManifestItems", LogLevel.Verbose))
            {
                unloaded = GetUnloadedManifestItems(hostServices, maxParallelism);
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
                    using (new LoggerFileScope(f.OriginalFileAndType.File))
                    {
                        var path = Context.GetFilePath(f.Key);
                        if (path != null)
                        {
                            if (!map.TryGetValue(f.OriginalFileAndType.File, out FileMapItem item))
                            {
                                map[f.OriginalFileAndType.File] = item = new FileMapItem();
                            }
                            item[f.Key] = path;
                        }
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
                    using (new LoggerFileScope(f.OriginalFileAndType.File))
                    {
                        if (f.Type == DocumentType.Overwrite)
                        {
                            map[f.OriginalFileAndType.File] = new List<XRefSpec>();
                        }
                        else
                        {
                            if (!map.TryGetValue(f.OriginalFileAndType.File, out List<XRefSpec> specs))
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
        }

        private List<ManifestItem> GetUnloadedManifestItems(IEnumerable<HostService> hostServices, int maxParallelism)
        {
            if (LastBuildVersionInfo == null)
            {
                return new List<ManifestItem>();
            }
            var unloadedFiles = (from h in hostServices
                                 where h.CanIncrementalBuild
                                 from f in h.GetUnloadedModelFiles(IncrementalContext)
                                 select f).Distinct(FilePathComparer.OSPlatformSensitiveStringComparer).ToDictionary(f => f, f => f, FilePathComparer.OSPlatformSensitiveStringComparer);

            using (new LoggerPhaseScope("UpdateItems", LogLevel.Verbose))
            {
                return (from mani in LastBuildVersionInfo.Manifest.AsParallel().WithDegreeOfParallelism(maxParallelism)
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

            // Copy when current base dir is not last base dir
            if (!FilePathComparerWithEnvironmentVariable.OSPlatformSensitiveRelativePathComparer.Equals(
                IncrementalContext.BaseDir,
                IncrementalContext.LastBaseDir))
            {
                foreach (var ofi in result.OutputFiles.Values)
                {
                    if (ofi.LinkToPath != null &&
                        ofi.LinkToPath.Length > IncrementalContext.LastBaseDir.Length &&
                        ofi.LinkToPath.StartsWith(IncrementalContext.LastBaseDir) &&
                        (ofi.LinkToPath[IncrementalContext.LastBaseDir.Length] == '\\' || ofi.LinkToPath[IncrementalContext.LastBaseDir.Length] == '/'))
                    {
                        IncrementalUtility.RetryIO(() =>
                        {
                            var path = Path.Combine(IncrementalContext.BaseDir, IncrementalUtility.GetRandomEntry(IncrementalContext.BaseDir));
                            File.Copy(Environment.ExpandEnvironmentVariables(ofi.LinkToPath), Environment.ExpandEnvironmentVariables(path));
                            ofi.LinkToPath = path;
                        });
                    }
                }
            }

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
