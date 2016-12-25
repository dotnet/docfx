﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    internal class IncrementalBuildContext
    {
        private DocumentBuildParameters _parameters;
        private Dictionary<string, Dictionary<string, BuildPhase?>> _modelLoadInfo = new Dictionary<string, Dictionary<string, BuildPhase?>>();
        private Dictionary<string, ChangeKindWithDependency> _changeDict = new Dictionary<string, ChangeKindWithDependency>();

        public string BaseDir { get; }

        public string LastBaseDir { get; }

        public DateTime? LastBuildStartTime { get; }

        public IncrementalInfo IncrementalInfo { get; }

        public BuildVersionInfo CurrentBuildVersionInfo { get; }

        public BuildVersionInfo LastBuildVersionInfo { get; }

        public bool CanVersionIncremental { get; }

        public string Version
        {
            get
            {
                return _parameters.VersionName;
            }
        }

        public IReadOnlyDictionary<string, Dictionary<string, BuildPhase?>> ModelLoadInfo
        {
            get
            {
                return _modelLoadInfo;
            }
        }

        public IReadOnlyDictionary<string, ChangeKindWithDependency> ChangeDict
        {
            get
            {
                return _changeDict;
            }
        }

        #region Creator and Constructor

        public static IncrementalBuildContext Create(DocumentBuildParameters parameters, BuildInfo cb, BuildInfo lb, string intermediateFolder, string markdownServiceContextHash)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (cb == null)
            {
                throw new ArgumentNullException(nameof(cb));
            }
            if (intermediateFolder == null)
            {
                throw new ArgumentNullException(nameof(intermediateFolder));
            }
            var baseDir = Path.Combine(intermediateFolder, cb.DirectoryName);
            var lastBaseDir = lb != null ? Path.Combine(intermediateFolder, lb.DirectoryName) : null;
            var lastBuildStartTime = lb?.BuildStartTime;
            var buildInfoIncrementalStatus = GetBuildInfoIncrementalStatus(cb, lb);
            var lbv = lb?.Versions?.SingleOrDefault(v => v.VersionName == parameters.VersionName);
            var cbv = new BuildVersionInfo
            {
                VersionName = parameters.VersionName,
                ConfigHash = ComputeConfigHash(parameters, markdownServiceContextHash),
                AttributesFile = IncrementalUtility.CreateRandomFileName(baseDir),
                DependencyFile = IncrementalUtility.CreateRandomFileName(baseDir),
                ManifestFile = IncrementalUtility.CreateRandomFileName(baseDir),
                OutputFile = IncrementalUtility.CreateRandomFileName(baseDir),
                XRefSpecMapFile = IncrementalUtility.CreateRandomFileName(baseDir),
                FileMapFile = IncrementalUtility.CreateRandomFileName(baseDir),
                BuildMessageFile = IncrementalUtility.CreateRandomFileName(baseDir),
                Attributes = ComputeFileAttributes(parameters, lbv?.Dependency),
                Dependency = ConstructDependencyGraphFromLast(lbv?.Dependency),
            };
            cb.Versions.Add(cbv);
            var context = new IncrementalBuildContext(baseDir, lastBaseDir, lastBuildStartTime, buildInfoIncrementalStatus, parameters, cbv, lbv);
            context.InitChanges();
            return context;
        }

        private IncrementalBuildContext(string baseDir, string lastBaseDir, DateTime? lastBuildStartTime, IncrementalStatus buildInfoIncrementalStatus, DocumentBuildParameters parameters, BuildVersionInfo cbv, BuildVersionInfo lbv)
        {
            _parameters = parameters;
            BaseDir = baseDir;
            LastBaseDir = lastBaseDir;
            LastBuildStartTime = lastBuildStartTime;
            CurrentBuildVersionInfo = cbv;
            LastBuildVersionInfo = lbv;
            IncrementalInfo = new IncrementalInfo();
            CanVersionIncremental = GetCanVersionIncremental(buildInfoIncrementalStatus);
        }

        #endregion

        #region Model Load Info

        /// <summary>
        /// report model load info
        /// </summary>
        /// <param name="hostService">host service</param>
        /// <param name="file">the model's LocalPathFromRoot</param>
        /// <param name="phase">the buildphase that the model was loaded at</param>
        public void ReportModelLoadInfo(HostService hostService, string file, BuildPhase? phase)
        {
            Dictionary<string, BuildPhase?> mi = null;
            string name = hostService.Processor.Name;
            if (!_modelLoadInfo.TryGetValue(name, out mi))
            {
                _modelLoadInfo[name] = mi = new Dictionary<string, BuildPhase?>();
            }
            mi[file] = phase;
        }

        /// <summary>
        /// report model load info
        /// </summary>
        /// <param name="hostService">host service</param>
        /// <param name="files">models' LocalPathFromRoot</param>
        /// <param name="phase">the buildphase that the model was loaded at</param>
        public void ReportModelLoadInfo(HostService hostService, IEnumerable<string> files, BuildPhase? phase)
        {
            foreach (var f in files)
            {
                ReportModelLoadInfo(hostService, f, phase);
            }
        }

        public IReadOnlyDictionary<string, BuildPhase?> GetModelLoadInfo(HostService hostService)
        {
            string name = hostService.Processor.Name;
            if (!hostService.ShouldTraceIncrementalInfo)
            {
                throw new InvalidOperationException($"HostService: {name} doesn't record incremental info, cannot call the method to get model load info.");
            }
            Dictionary<string, BuildPhase?> mi;
            if (ModelLoadInfo.TryGetValue(name, out mi))
            {
                return mi;
            }
            return new Dictionary<string, BuildPhase?>();
        }

        #endregion

        #region Intermediate Files
        public ModelManifest GetCurrentIntermediateModelManifest(HostService hostService)
        {
            return CurrentBuildVersionInfo?.Processors?.Find(p => p.Name == hostService.Processor.Name)?.IntermediateModelManifest;
        }

        public ModelManifest GetLastIntermediateModelManifest(HostService hostService)
        {
            return LastBuildVersionInfo?.Processors?.Find(p => p.Name == hostService.Processor.Name)?.IntermediateModelManifest;
        }

        #endregion

        #region Changes

        public void LoadChanges()
        {
            if (LastBuildVersionInfo == null)
            {
                throw new InvalidOperationException("Only incremental build could load changes.");
            }

            var lastFileAttributes = LastBuildVersionInfo.Attributes;
            if (_parameters.Changes != null)
            {
                // use user-provided changelist
                foreach (var pair in _parameters.Changes)
                {
                    _changeDict[pair.Key] = pair.Value;
                }

                // scenario: file itself doesn't change but add/remove from docfx.json
                var lastSrcFiles = (from p in lastFileAttributes
                                    where p.Value.IsFromSource == true
                                    select p.Key).ToList();
                foreach (var file in _parameters.Changes.Keys.Except(lastSrcFiles))
                {
                    if (_changeDict[file] == ChangeKindWithDependency.None)
                    {
                        _changeDict[file] = ChangeKindWithDependency.Created;
                    }
                }
                foreach (var file in lastSrcFiles.Except(_parameters.Changes.Keys))
                {
                    _changeDict[file] = ChangeKindWithDependency.Deleted;
                }
            }
            else
            {
                // get changelist from lastBuildInfo if user doesn't provide changelist
                var fileAttributes = CurrentBuildVersionInfo.Attributes;
                DateTime checkTime = LastBuildStartTime.Value;
                foreach (var file in fileAttributes.Keys.Intersect(lastFileAttributes.Keys))
                {
                    var last = lastFileAttributes[file];
                    var current = fileAttributes[file];
                    if (current.IsFromSource && !last.IsFromSource)
                    {
                        _changeDict[file] = ChangeKindWithDependency.Created;
                    }
                    else if (!current.IsFromSource && last.IsFromSource)
                    {
                        _changeDict[file] = ChangeKindWithDependency.Deleted;
                    }
                    else if (current.LastModifiedTime > checkTime && current.MD5 != last.MD5)
                    {
                        _changeDict[file] = ChangeKindWithDependency.Updated;
                    }
                    else
                    {
                        _changeDict[file] = ChangeKindWithDependency.None;
                    }
                }

                foreach (var file in lastFileAttributes.Keys.Except(fileAttributes.Keys))
                {
                    _changeDict[file] = ChangeKindWithDependency.Deleted;
                }
                foreach (var file in fileAttributes.Keys.Except(lastFileAttributes.Keys))
                {
                    _changeDict[file] = ChangeKindWithDependency.Created;
                }
            }
        }

        public List<string> ExpandDependency(Func<DependencyItem, bool> isValid)
        {
            var newChanges = new List<string>();
            var dependencyGraph = CurrentBuildVersionInfo.Dependency;

            if (dependencyGraph != null)
            {
                foreach (var from in dependencyGraph.FromNodes)
                {
                    if (dependencyGraph.GetAllDependencyFrom(from).Any(d => isValid(d) && _changeDict.ContainsKey(d.To) && _changeDict[d.To] != ChangeKindWithDependency.None))
                    {
                        if (!_changeDict.ContainsKey(from))
                        {
                            _changeDict[from] = ChangeKindWithDependency.DependencyUpdated;
                            newChanges.Add(from);
                        }
                        else
                        {
                            if (_changeDict[from] == ChangeKindWithDependency.None)
                            {
                                newChanges.Add(from);
                            }
                            _changeDict[from] |= ChangeKindWithDependency.DependencyUpdated;
                        }
                    }
                }
            }
            return newChanges;
        }

        #endregion

        #region BuildVersionInfo

        public void UpdateBuildVersionInfoPerDependencyGraph()
        {
            if (CurrentBuildVersionInfo.Dependency == null)
            {
                return;
            }
            var fileAttributes = CurrentBuildVersionInfo.Attributes;
            var nodesToUpdate = CurrentBuildVersionInfo.Dependency.GetAllDependentNodes().Except(fileAttributes.Keys);
            foreach (var node in nodesToUpdate)
            {
                RelativePath path = RelativePath.TryParse(node);
                if (path == null)
                {
                    continue;
                }
                string fullPath = Path.Combine(EnvironmentContext.BaseDirectory, path.RemoveWorkingFolder());
                if (File.Exists(fullPath))
                {
                    fileAttributes[node] = new FileAttributeItem
                    {
                        File = node,
                        LastModifiedTime = File.GetLastWriteTimeUtc(fullPath),
                        MD5 = StringExtension.GetMd5String(File.ReadAllText(fullPath)),
                    };
                }
            }
        }

        public ProcessorInfo CreateProcessorInfo(IDocumentProcessor processor)
        {
            var cpi = new ProcessorInfo
            {
                Name = processor.Name,
                IncrementalContextHash = ((ISupportIncrementalDocumentProcessor)processor).GetIncrementalContextHash(),
            };
            foreach (var step in processor.BuildSteps)
            {
                cpi.Steps.Add(new ProcessorStepInfo
                {
                    Name = step.Name,
                    IncrementalContextHash = ((ISupportIncrementalBuildStep)step).GetIncrementalContextHash(),
                });
            }
            CurrentBuildVersionInfo.Processors.Add(cpi);
            return cpi;
        }

        public bool ShouldProcessorTraceInfo(IDocumentProcessor processor)
        {
            if (!(processor is ISupportIncrementalDocumentProcessor))
            {
                string message = $"Processor {processor.Name} cannot suppport incremental build because the processor doesn't implement {nameof(ISupportIncrementalDocumentProcessor)} interface.";
                IncrementalInfo.ReportProcessorStatus(processor.Name, false, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (!processor.BuildSteps.All(step => step is ISupportIncrementalBuildStep))
            {
                string message = $"Processor {processor.Name} cannot suppport incremental build because the following steps don't implement {nameof(ISupportIncrementalBuildStep)} interface: {string.Join(",", processor.BuildSteps.Where(step => !(step is ISupportIncrementalBuildStep)).Select(s => s.Name))}.";
                IncrementalInfo.ReportProcessorStatus(processor.Name, false, message);
                Logger.LogVerbose(message);
                return false;
            }
            return true;
        }

        public bool CanProcessorIncremental(IDocumentProcessor processor)
        {
            if (!CanVersionIncremental)
            {
                IncrementalInfo.ReportProcessorStatus(processor.Name, false);
                return false;
            }

            var cpi = CurrentBuildVersionInfo.Processors.Find(p => p.Name == processor.Name);
            if (cpi == null)
            {
                string message = $"Current BuildVersionInfo missed processor info for {processor.Name}.";
                IncrementalInfo.ReportProcessorStatus(processor.Name, false, message);
                Logger.LogWarning(message);
                return false;
            }
            var lpi = LastBuildVersionInfo.Processors.Find(p => p.Name == processor.Name);
            if (lpi == null)
            {
                string message = $"Processor {processor.Name} disable incremental build because last build doesn't contain version {Version}.";
                IncrementalInfo.ReportProcessorStatus(processor.Name, false, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (cpi.IncrementalContextHash != lpi.IncrementalContextHash)
            {
                string message = $"Processor {processor.Name} disable incremental build because incremental context hash changed.";
                IncrementalInfo.ReportProcessorStatus(processor.Name, false, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (cpi.Steps.Count != lpi.Steps.Count)
            {
                string message = $"Processor {processor.Name} disable incremental build because steps count is different.";
                IncrementalInfo.ReportProcessorStatus(processor.Name, false, message);
                Logger.LogVerbose(message);
                return false;
            }
            for (int i = 0; i < cpi.Steps.Count; i++)
            {
                if (!object.Equals(cpi.Steps[i], lpi.Steps[i]))
                {
                    string message = $"Processor {processor.Name} disable incremental build because steps changed, from step {lpi.Steps[i].ToJsonString()} to {cpi.Steps[i].ToJsonString()}.";
                    IncrementalInfo.ReportProcessorStatus(processor.Name, false, message);
                    Logger.LogVerbose(message);
                    return false;
                }
            }
            IncrementalInfo.ReportProcessorStatus(processor.Name, true);
            Logger.LogVerbose($"Processor {processor.Name} enable incremental build.");
            return true;
        }

        #endregion

        #region Private Methods

        private static string ComputeConfigHash(DocumentBuildParameters parameter, string markdownServiceContextHash)
        {
            using (new LoggerPhaseScope("ComputeConfigHash", true))
            {
                var json = JsonConvert.SerializeObject(
                parameter,
                new JsonSerializerSettings
                {
                    ContractResolver = new IncrementalIgnorePropertiesResolver()
                });
                var config = json + "|" + markdownServiceContextHash;
                Logger.LogVerbose($"Config content: {config}");
                return config.GetMd5String();
            }
        }

        private static Dictionary<string, FileAttributeItem> ComputeFileAttributes(DocumentBuildParameters parameters, DependencyGraph dg)
        {
            using (new LoggerPhaseScope("ComputeFileAttributes", true))
            {
                var filesInScope = from f in parameters.Files.EnumerateFiles()
                                   let fileKey = ((RelativePath)f.File).GetPathFromWorkingFolder().ToString()
                                   select new
                                   {
                                       PathFromWorkingFolder = fileKey,
                                       FullPath = f.FullPath,
                                       IsFromSource = true,
                                   };
                var files = filesInScope;
                if (dg != null)
                {
                    var filesFromDependency = from node in dg.GetAllDependentNodes()
                                              let p = RelativePath.TryParse(node)
                                              where p != null
                                              let fullPath = Path.Combine(EnvironmentContext.BaseDirectory, p.RemoveWorkingFolder())
                                              select new
                                              {
                                                  PathFromWorkingFolder = node,
                                                  FullPath = fullPath,
                                                  IsFromSource = false,
                                              };
                    files = files.Concat(filesFromDependency);
                }

                return (from item in files
                        where File.Exists(item.FullPath)
                        group item by item.PathFromWorkingFolder into g
                        select new FileAttributeItem
                        {
                            File = g.Key,
                            LastModifiedTime = File.GetLastWriteTimeUtc(g.First().FullPath),
                            MD5 = StringExtension.GetMd5String(File.ReadAllText(g.First().FullPath)),
                            IsFromSource = g.Any(v => v.IsFromSource),
                        }).ToDictionary(a => a.File);
            }
        }

        private static DependencyGraph ConstructDependencyGraphFromLast(DependencyGraph ldg)
        {
            using (new LoggerPhaseScope("ConstructDgFromLast", true))
            {
                var dg = new DependencyGraph();
                if (ldg == null)
                {
                    return dg;
                }

                // reregister dependency types from last dependency graph
                using (new LoggerPhaseScope("RegisterDependencyTypeFromLastBuild", true))
                {
                    dg.RegisterDependencyType(ldg.DependencyTypes.Values);
                }

                // restore dependency graph from last dependency graph
                using (new LoggerPhaseScope("ReportDependencyFromLastBuild", true))
                {
                    dg.ReportDependency(from r in ldg.ReportedBys
                                        from i in ldg.GetDependencyReportedBy(r)
                                        select i);
                }
                return dg;
            }
        }

        private static IncrementalStatus GetBuildInfoIncrementalStatus(BuildInfo cb, BuildInfo lb)
        {
            if (lb == null)
            {
                return new IncrementalStatus { CanIncremental = false, Details = "Cannot build incrementally because last build info is missing." };
            }
            if (cb.DocfxVersion != lb.DocfxVersion)
            {
                return new IncrementalStatus { CanIncremental = false, Details = $"Cannot build incrementally because docfx version changed from {lb.DocfxVersion} to {cb.DocfxVersion}." };
            }
            if (cb.PluginHash != lb.PluginHash)
            {
                return new IncrementalStatus { CanIncremental = false, Details = "Cannot build incrementally because plugin changed." };
            }
            if (cb.TemplateHash != lb.TemplateHash)
            {
                return new IncrementalStatus { CanIncremental = false, Details = "Cannot build incrementally because template changed." };
            }
            if (cb.CommitFromSHA != lb.CommitToSHA)
            {
                return new IncrementalStatus { CanIncremental = false, Details = $"Cannot build incrementally because commit SHA doesn't match. Last build commit: {lb.CommitToSHA}. Current build commit base: {cb.CommitFromSHA}." };
            }
            return new IncrementalStatus { CanIncremental = true };
        }

        private bool GetCanVersionIncremental(IncrementalStatus buildInfoIncrementalStatus)
        {
            if (!buildInfoIncrementalStatus.CanIncremental)
            {
                IncrementalInfo.ReportStatus(false, buildInfoIncrementalStatus.Details);
                Logger.LogVerbose(buildInfoIncrementalStatus.Details);
                return false;
            }
            if (LastBuildVersionInfo == null)
            {
                string message = $"Cannot build incrementally because last build didn't contain version {Version}.";
                IncrementalInfo.ReportStatus(false, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (CurrentBuildVersionInfo.ConfigHash != LastBuildVersionInfo.ConfigHash)
            {
                string message = "Cannot build incrementally because config changed.";
                IncrementalInfo.ReportStatus(false, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (_parameters.ForceRebuild)
            {
                string message = $"Disable incremental build by force rebuild option.";
                IncrementalInfo.ReportStatus(false, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (_parameters.ApplyTemplateSettings != null)
            {
                var options = _parameters.ApplyTemplateSettings.Options;
                if ((options & (ApplyTemplateOptions.ExportRawModel | ApplyTemplateOptions.ExportViewModel)) != ApplyTemplateOptions.None)
                {
                    string message = $"Disable incremental build because ExportRawModel/ExportViewModel option enabled.";
                    IncrementalInfo.ReportStatus(false, message);
                    Logger.LogVerbose(message);
                    return false;
                }
            }
            IncrementalInfo.ReportStatus(true);
            return true;
        }

        private void InitChanges()
        {
            if (CanVersionIncremental)
            {
                using (new LoggerPhaseScope("LoadChanges", true))
                {
                    LoadChanges();
                }
                Logger.LogVerbose($"Before expanding dependency before build, changes: {JsonUtility.Serialize(ChangeDict, Formatting.Indented)}");
                using (new LoggerPhaseScope("ExpandDependency", true))
                {
                    ExpandDependency(d => CurrentBuildVersionInfo.Dependency.DependencyTypes[d.Type].Phase == BuildPhase.Compile || CurrentBuildVersionInfo.Dependency.DependencyTypes[d.Type].TriggerBuild);
                }
                Logger.LogVerbose($"After expanding dependency before build, changes: {JsonUtility.Serialize(ChangeDict, Formatting.Indented)}");
            }
        }

        #endregion
    }
}
