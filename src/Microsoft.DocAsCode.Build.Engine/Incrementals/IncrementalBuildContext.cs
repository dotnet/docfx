﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    internal class IncrementalBuildContext
    {
        private readonly object _sync = new object();
        private DocumentBuildParameters _parameters;
        private Dictionary<string, OSPlatformSensitiveDictionary<BuildPhase?>> _modelLoadInfo = new Dictionary<string, OSPlatformSensitiveDictionary<BuildPhase?>>();
        private OSPlatformSensitiveDictionary<ChangeKindWithDependency> _changeDict = new OSPlatformSensitiveDictionary<ChangeKindWithDependency>();

        public string BaseDir { get; }

        public string LastBaseDir { get; }

        public DateTime? LastBuildStartTime { get; }

        public IncrementalInfo IncrementalInfo { get; }

        public BuildVersionInfo CurrentBuildVersionInfo { get; }

        public BuildVersionInfo LastBuildVersionInfo { get; }

        public bool CanVersionIncremental { get; }

        public bool IsTemplateUpdated { get; private set; }

        public string Version
        {
            get
            {
                return _parameters.VersionName;
            }
        }

        public IReadOnlyDictionary<string, OSPlatformSensitiveDictionary<BuildPhase?>> ModelLoadInfo
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
            var cbv = new BuildVersionInfo()
            {
                BaseDir = Path.GetFullPath(Environment.ExpandEnvironmentVariables(baseDir)),
                VersionName = parameters.VersionName,
                ConfigHash = ComputeConfigHash(parameters, markdownServiceContextHash),
                AttributesFile = IncrementalUtility.CreateRandomFileName(baseDir),
                DependencyFile = IncrementalUtility.CreateRandomFileName(baseDir),
                ManifestFile = IncrementalUtility.CreateRandomFileName(baseDir),
                OutputFile = IncrementalUtility.CreateRandomFileName(baseDir),
                XRefSpecMapFile = IncrementalUtility.CreateRandomFileName(baseDir),
                FileMapFile = IncrementalUtility.CreateRandomFileName(baseDir),
                BuildMessageFile = IncrementalUtility.CreateRandomFileName(baseDir),
                TocRestructionsFile = IncrementalUtility.CreateRandomFileName(baseDir),
            };
            cb.Versions.Add(cbv);
            var context = new IncrementalBuildContext(baseDir, lastBaseDir, lastBuildStartTime, buildInfoIncrementalStatus, parameters, cbv, lbv)
            {
                IsTemplateUpdated = (cb.TemplateHash != lb?.TemplateHash)
            };
            context.InitDependency();
            context.InitFileAttributes();
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
            OSPlatformSensitiveDictionary<BuildPhase?> mi = null;
            string name = hostService.Processor.Name;
            lock (_modelLoadInfo)
            {
                if (!_modelLoadInfo.TryGetValue(name, out mi))
                {
                    _modelLoadInfo[name] = mi = new OSPlatformSensitiveDictionary<BuildPhase?>();
                }
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
            if (ModelLoadInfo.TryGetValue(name, out OSPlatformSensitiveDictionary<BuildPhase?> mi))
            {
                return mi;
            }
            return new OSPlatformSensitiveDictionary<BuildPhase?>();
        }

        public ImmutableDictionary<string, FileIncrementalInfo> GetModelIncrementalInfo(HostService hostService, BuildPhase phase)
        {
            if (hostService == null)
            {
                throw new ArgumentNullException(nameof(hostService));
            }
            if (!hostService.ShouldTraceIncrementalInfo)
            {
                throw new InvalidOperationException($"HostService: {hostService.Processor.Name} doesn't record incremental info, cannot call the method to get model incremental info.");
            }
            var increInfo = (from pair in GetModelLoadInfo(hostService)
                             let incr = pair.Value == null ? true : false
                             select new FileIncrementalInfo
                             {
                                 SourceFile = pair.Key,
                                 IsIncremental = incr,
                             }).ToImmutableDictionary(f => f.SourceFile, f => f, FilePathComparer.OSPlatformSensitiveStringComparer);

            return increInfo;
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
                foreach (var file in _parameters.Changes.Keys.Except(lastSrcFiles, FilePathComparer.OSPlatformSensitiveStringComparer))
                {
                    if (_changeDict[file] == ChangeKindWithDependency.None)
                    {
                        _changeDict[file] = ChangeKindWithDependency.Created;
                    }
                }
                foreach (var file in lastSrcFiles.Except(_parameters.Changes.Keys, FilePathComparer.OSPlatformSensitiveStringComparer))
                {
                    _changeDict[file] = ChangeKindWithDependency.Deleted;
                }
            }
            else
            {
                // get changelist from lastBuildInfo if user doesn't provide changelist
                var fileAttributes = CurrentBuildVersionInfo.Attributes;
                DateTime checkTime = LastBuildStartTime.Value;
                foreach (var file in fileAttributes.Keys.Intersect(lastFileAttributes.Keys, FilePathComparer.OSPlatformSensitiveStringComparer))
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

                foreach (var file in lastFileAttributes.Keys.Except(fileAttributes.Keys, FilePathComparer.OSPlatformSensitiveStringComparer))
                {
                    _changeDict[file] = ChangeKindWithDependency.Deleted;
                }
                foreach (var file in fileAttributes.Keys.Except(lastFileAttributes.Keys, FilePathComparer.OSPlatformSensitiveStringComparer))
                {
                    _changeDict[file] = ChangeKindWithDependency.Created;
                }
            }
        }

        public List<string> ExpandDependency(DependencyGraph dg, Func<DependencyItem, bool> isValid)
        {
            var newChanges = new List<string>();

            if (dg != null)
            {
                foreach (var change in (from c in _changeDict
                                        where c.Value != ChangeKindWithDependency.None
                                        where c.Value != ChangeKindWithDependency.DependencyUpdated
                                        select c).ToList())
                {
                    foreach (var dt in dg.GetAllDependencyTo(change.Key))
                    {
                        if (!isValid(dt))
                        {
                            continue;
                        }
                        string from = dt.From.Value;
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

        public void InitFileAttributes()
        {
            using (new LoggerPhaseScope("InitFileAttributes", LogLevel.Verbose))
            {
                var fileAttributes = CurrentBuildVersionInfo.Attributes;
                foreach (var f in GetFilesToCalculateAttributes())
                {
                    string key = f.PathFromWorkingFolder;
                    if (fileAttributes.ContainsKey(key))
                    {
                        continue;
                    }
                    if (!TryGetFileAttributeFromLast(key, out FileAttributeItem item))
                    {
                        string md5;
                        using (var fs = File.OpenRead(f.FullPath))
                        {
                            md5 = Convert.ToBase64String(MD5.Create().ComputeHash(fs));
                        }
                        fileAttributes[key] = new FileAttributeItem
                        {
                            File = key,
                            LastModifiedTime = File.GetLastWriteTimeUtc(f.FullPath),
                            MD5 = md5,
                            IsFromSource = f.IsFromSource,
                        };
                    }
                    else
                    {
                        fileAttributes[key] = new FileAttributeItem
                        {
                            File = item.File,
                            LastModifiedTime = item.LastModifiedTime,
                            MD5 = item.MD5,
                            IsFromSource = f.IsFromSource,
                        };
                    }
                }
            }
        }

        public void UpdateBuildVersionInfoPerDependencyGraph()
        {
            if (CurrentBuildVersionInfo.Dependency == null)
            {
                return;
            }
            var fileAttributes = CurrentBuildVersionInfo.Attributes;
            var nodesToUpdate = CurrentBuildVersionInfo.Dependency.GetAllDependentNodes().Except(fileAttributes.Keys, FilePathComparer.OSPlatformSensitiveStringComparer);
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
                    ContextInfoFile = (step is ICanTraceContextInfoBuildStep) ? IncrementalUtility.CreateRandomFileName(BaseDir) : null,
                });
            }
            lock (_sync)
            {
                CurrentBuildVersionInfo.Processors.Add(cpi);
            }
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

        #region Plugin Context info

        public void LoadContextInfo(HostService hostService)
        {
            if (hostService == null)
            {
                throw new ArgumentNullException(nameof(hostService));
            }
            if (!hostService.CanIncrementalBuild)
            {
                return;
            }
            var lpi = LastBuildVersionInfo.Processors.Find(p => p.Name == hostService.Processor.Name);
            if (lpi == null)
            {
                return;
            }

            foreach (var step in hostService.Processor.BuildSteps.OfType<ICanTraceContextInfoBuildStep>())
            {
                var stepInfo = lpi.Steps.Find(s => s.Name == step.Name);
                if (stepInfo == null || stepInfo.ContextInfoFile == null)
                {
                    continue;
                }
                using (var stream = File.OpenRead(Path.Combine(Environment.ExpandEnvironmentVariables(LastBaseDir), stepInfo.ContextInfoFile)))
                {
                    step.LoadContext(stream);
                }
            }
        }

        public void SaveContextInfo(HostService hostService)
        {
            if (hostService == null)
            {
                throw new ArgumentNullException(nameof(hostService));
            }
            if (!hostService.ShouldTraceIncrementalInfo)
            {
                return;
            }
            var lpi = CurrentBuildVersionInfo.Processors.Find(p => p.Name == hostService.Processor.Name);
            if (lpi == null)
            {
                return;
            }

            foreach (var step in hostService.Processor.BuildSteps.OfType<ICanTraceContextInfoBuildStep>())
            {
                var stepInfo = lpi.Steps.Find(s => s.Name == step.Name);
                if (stepInfo == null || stepInfo.ContextInfoFile == null)
                {
                    continue;
                }
                using (var stream = File.Create(Path.Combine(Environment.ExpandEnvironmentVariables(BaseDir), stepInfo.ContextInfoFile)))
                {
                    step.SaveContext(stream);
                }
            }
        }

        #endregion

        #region Private Methods

        private static string ComputeConfigHash(DocumentBuildParameters parameter, string markdownServiceContextHash)
        {
            using (new LoggerPhaseScope("ComputeConfigHash", LogLevel.Diagnostic))
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

        private static DependencyGraph ConstructDependencyGraphFromLast(DependencyGraph ldg)
        {
            using (new LoggerPhaseScope("ConstructDgFromLast", LogLevel.Verbose))
            {
                var dg = new DependencyGraph();
                if (ldg == null)
                {
                    return dg;
                }

                // reregister dependency types from last dependency graph
                using (new LoggerPhaseScope("RegisterDependencyTypeFromLastBuild", LogLevel.Diagnostic))
                {
                    dg.RegisterDependencyType(ldg.DependencyTypes.Values);
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
                IncrementalInfo.ReportStatus(false, IncrementalPhase.Build, buildInfoIncrementalStatus.Details);
                Logger.LogVerbose(buildInfoIncrementalStatus.Details);
                return false;
            }
            if (LastBuildVersionInfo == null)
            {
                string message = $"Cannot build incrementally because last build didn't contain version {Version}.";
                IncrementalInfo.ReportStatus(false, IncrementalPhase.Build, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (CurrentBuildVersionInfo.ConfigHash != LastBuildVersionInfo.ConfigHash)
            {
                string message = "Cannot build incrementally because config changed.";
                IncrementalInfo.ReportStatus(false, IncrementalPhase.Build, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (_parameters.ForceRebuild)
            {
                string message = $"Disable incremental build by force rebuild option.";
                IncrementalInfo.ReportStatus(false, IncrementalPhase.Build, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (_parameters.ApplyTemplateSettings != null)
            {
                var options = _parameters.ApplyTemplateSettings.Options;
                if ((options & (ApplyTemplateOptions.ExportRawModel | ApplyTemplateOptions.ExportViewModel)) != ApplyTemplateOptions.None)
                {
                    string message = $"Disable incremental build because ExportRawModel/ExportViewModel option enabled.";
                    IncrementalInfo.ReportStatus(false, IncrementalPhase.Build, message);
                    Logger.LogVerbose(message);
                    return false;
                }
            }
            IncrementalInfo.ReportStatus(true, IncrementalPhase.Build);
            return true;
        }

        private IEnumerable<FileItem> GetFilesToCalculateAttributes()
        {
            var keys = new HashSet<string>(FilePathComparer.OSPlatformSensitiveStringComparer);
            foreach (var f in _parameters.Files.EnumerateFiles())
            {
                var fileKey = ((RelativePath)f.File).GetPathFromWorkingFolder().ToString();
                keys.Add(fileKey);
                yield return new FileItem
                {
                    PathFromWorkingFolder = fileKey,
                    FullPath = f.FullPath,
                    IsFromSource = true,
                };
            }

            if (CanVersionIncremental)
            {
                var dependency = LastBuildVersionInfo.Dependency;
                foreach (var f in dependency.GetAllDependentNodes())
                {
                    if (keys.Contains(f))
                    {
                        continue;
                    }
                    string fullPath = TryGetFullPath(f);
                    if (fullPath != null && File.Exists(fullPath))
                    {
                        yield return new FileItem
                        {
                            PathFromWorkingFolder = f,
                            FullPath = fullPath,
                            IsFromSource = false,
                        };
                    }
                }
            }
        }

        private string TryGetFullPath(string path)
        {
            string fullPath = null;
            try
            {
                var p = RelativePath.TryParse(path);
                if (p == null)
                {
                    return null;
                }
                fullPath = PathUtility.GetFullPath(EnvironmentContext.BaseDirectory, p.RemoveWorkingFolder());
            }
            catch (ArgumentException)
            {
                // ignore the file if it contains illegal characters
            }
            catch (Exception ex)
            {
                Logger.LogVerbose($"Failed to get full path for: {path}. Exception details: {ex.Message}.");
            }
            return fullPath;
        }

        private bool TryGetFileAttributeFromLast(string pathFromWorkingFolder, out FileAttributeItem item)
        {
            item = null;
            if (!CanVersionIncremental)
            {
                return false;
            }
            if (_parameters.Changes == null)
            {
                return false;
            }
            if (_parameters.Changes.ContainsKey(pathFromWorkingFolder) && _parameters.Changes[pathFromWorkingFolder] != ChangeKindWithDependency.None)
            {
                return false;
            }
            if (LastBuildVersionInfo == null)
            {
                return false;
            }
            if (!LastBuildVersionInfo.Attributes.TryGetValue(pathFromWorkingFolder, out item))
            {
                return false;
            }
            return true;
        }

        private void InitDependency()
        {
            if (CanVersionIncremental)
            {
                CurrentBuildVersionInfo.Dependency = ConstructDependencyGraphFromLast(LastBuildVersionInfo.Dependency);
            }
            else
            {
                CurrentBuildVersionInfo.Dependency = new DependencyGraph();
            }
        }

        private void InitChanges()
        {
            if (CanVersionIncremental)
            {
                using (new LoggerPhaseScope("LoadChanges", LogLevel.Diagnostic))
                {
                    LoadChanges();
                }
                Logger.LogDiagnostic($"Before expanding dependency before build, changes: {JsonUtility.Serialize(ChangeDict, Formatting.Indented)}");
                using (new LoggerPhaseScope("ExpandDependency", LogLevel.Diagnostic))
                {
                    ExpandDependency(LastBuildVersionInfo?.Dependency, d => CurrentBuildVersionInfo.Dependency.DependencyTypes[d.Type].Phase == BuildPhase.Compile);
                }
                Logger.LogDiagnostic($"After expanding dependency before build, changes: {JsonUtility.Serialize(ChangeDict, Formatting.Indented)}");
            }
        }

        #endregion

        private class FileItem
        {
            public string PathFromWorkingFolder { get; set; }

            public string FullPath { get; set; }

            public bool IsFromSource { get; set; }
        }
    }
}
