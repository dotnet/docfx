// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdigEngine;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IHostService))]
    internal sealed class HostService : IHostService, IDisposable
    {
        #region Fields
        private readonly object _syncRoot = new object();
        private readonly object _tocSyncRoot = new object();
        private readonly Dictionary<string, List<FileModel>> _uidIndex = new Dictionary<string, List<FileModel>>();
        private readonly LruList<ModelWithCache> _lru;
        #endregion

        #region Properties

        public IBuildParameters BuildParameters { get; }

        public TemplateProcessor Template { get; set; }

        public ImmutableList<FileModel> Models { get; private set; }

        public ImmutableDictionary<string, FileAndType> SourceFiles { get; set; }

        public ImmutableList<string> InvalidSourceFiles { get; set; }

        public ImmutableDictionary<string, FileIncrementalInfo> IncrementalInfos { get; set; }

        public Dictionary<FileAndType, FileAndType> FileMap { get; } = new Dictionary<FileAndType, FileAndType>();

        public IMarkdownService MarkdownService { get; set; }

        public ImmutableList<IInputMetadataValidator> Validators { get; set; }

        public DependencyGraph DependencyGraph { get; set; }

        public bool ShouldTraceIncrementalInfo { get; set; }

        public bool CanIncrementalBuild { get; set; }

        public ImmutableList<TreeItemRestructure> TableOfContentRestructions { get; set; }

        public string VersionName { get; }

        public string VersionOutputFolder { get; }

        public GroupInfo GroupInfo { get; }

        #endregion

        #region Constructors

        public HostService(string baseDir, IEnumerable<FileModel> models)
            : this(baseDir, models, null, null, 0, null) { }

        public HostService(string baseDir, IEnumerable<FileModel> models, string versionName, string versionDir, int lruSize)
            : this(baseDir, models, versionName, versionDir, lruSize, null, null) { }

        public HostService(string baseDir, IEnumerable<FileModel> models, string versionName, string versionDir, int lruSize, GroupInfo groupInfo)
            : this(baseDir, models, versionName, versionDir, lruSize, groupInfo, null) { }

        public HostService(string baseDir, IEnumerable<FileModel> models, string versionName, string versionDir, int lruSize, GroupInfo groupInfo, IBuildParameters buildParameters)
        {
            VersionName = versionName;
            VersionOutputFolder = versionDir;
            GroupInfo = groupInfo;
            BuildParameters = buildParameters;

            // Disable LRU, when Content.get, it is possible that the value is Serialized before the modification on the content does not complete yet
            //if (lruSize > 0)
            //{
            //    _lru = LruList<ModelWithCache>.CreateSynchronized(lruSize, OnLruRemoving);
            //}
            LoadCore(models);
        }

        #endregion

        #region IHostService Members

        public IDocumentProcessor Processor { get; set; }

        public ImmutableList<FileModel> GetModels(DocumentType? type)
        {
            if (type == null)
            {
                return Models;
            }
            return (from m in Models where m.Type == type select m).ToImmutableList();
        }

        public ImmutableHashSet<string> GetAllUids()
        {
            lock (_syncRoot)
            {
                return _uidIndex.Keys.ToImmutableHashSet();
            }
        }

        public ImmutableList<FileModel> LookupByUid(string uid)
        {
            if (uid == null)
            {
                throw new ArgumentNullException(nameof(uid));
            }
            lock (_syncRoot)
            {
                if (_uidIndex.TryGetValue(uid, out List<FileModel> result))
                {
                    return result.ToImmutableList();
                }
                return ImmutableList<FileModel>.Empty;
            }
        }

        public MarkupResult Markup(string markdown, FileAndType ft)
        {
            return Markup(markdown, ft, false);
        }

        public MarkupResult Markup(string markdown, FileAndType ft, bool omitParse)
        {
            return Markup(markdown, ft, omitParse, false);
        }

        public MarkupResult Markup(string markdown, FileAndType ft, bool omitParse, bool enableValidation)
        {
            if (markdown == null)
            {
                throw new ArgumentNullException(nameof(markdown));
            }
            if (ft == null)
            {
                throw new ArgumentNullException(nameof(ft));
            }
            return MarkupCore(markdown, ft, omitParse, enableValidation);
        }

        public MarkupResult Parse(MarkupResult markupResult, FileAndType ft)
        {
            return MarkupUtility.Parse(markupResult, ft.File, SourceFiles);
        }

        private MarkupResult MarkupCore(string markdown, FileAndType ft, bool omitParse, bool enableValidation)
        {
            try
            {
                var mr = MarkdownService is MarkdigMarkdownService
                    ? MarkdownService.Markup(markdown, ft.File, enableValidation)
                    : MarkdownService.Markup(markdown, ft.File);
                if (omitParse)
                {
                    return mr;
                }
                return Parse(mr, ft);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Fail("Markup failed!");
                var message = $"Markup failed: {ex.Message}.";
                Logger.LogError(message);
                throw new DocumentException(message, ex);
            }
        }

        public void ReportDependencyTo(FileModel currentFileModel, string to, string type)
        {
            ReportDependencyTo(currentFileModel, to, DependencyItemSourceType.File, type);
        }

        public void ReportDependencyTo(FileModel currentFileModel, string to, string toType, string type)
        {
            if (currentFileModel == null)
            {
                throw new ArgumentNullException(nameof(currentFileModel));
            }
            if (string.IsNullOrEmpty(to))
            {
                throw new ArgumentNullException(nameof(to));
            }
            if (toType == null)
            {
                throw new ArgumentNullException(nameof(toType));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (DependencyGraph == null)
            {
                return;
            }
            string fromKey = IncrementalUtility.GetDependencyKey(currentFileModel.OriginalFileAndType);
            string toKey = toType == DependencyItemSourceType.File ?
                IncrementalUtility.GetDependencyKey(currentFileModel.OriginalFileAndType.ChangeFile((RelativePath)currentFileModel.OriginalFileAndType.File + (RelativePath)to)) :
                to;
            ReportDependencyCore(fromKey, new DependencyItemSourceInfo(toType, toKey), fromKey, type);
        }

        public void ReportDependencyFrom(FileModel currentFileModel, string from, string type)
        {
            ReportDependencyFrom(currentFileModel, from, DependencyItemSourceType.File, type);
        }

        public void ReportDependencyFrom(FileModel currentFileModel, string from, string fromType, string type)
        {
            if (currentFileModel == null)
            {
                throw new ArgumentNullException(nameof(currentFileModel));
            }
            if (string.IsNullOrEmpty(from))
            {
                throw new ArgumentNullException(nameof(from));
            }
            if (fromType == null)
            {
                throw new ArgumentNullException(nameof(fromType));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (DependencyGraph == null)
            {
                return;
            }
            string fromKey = fromType == DependencyItemSourceType.File ?
                IncrementalUtility.GetDependencyKey(currentFileModel.OriginalFileAndType.ChangeFile((RelativePath)currentFileModel.OriginalFileAndType.File + (RelativePath)from)) :
                from;
            string toKey = IncrementalUtility.GetDependencyKey(currentFileModel.OriginalFileAndType);
            ReportDependencyCore(new DependencyItemSourceInfo(fromType, fromKey), toKey, toKey, type);
        }

        public void ReportReference(FileModel currentFileModel, string reference, string referenceType)
        {
            if (currentFileModel == null)
            {
                throw new ArgumentNullException(nameof(currentFileModel));
            }
            if (string.IsNullOrEmpty(reference))
            {
                throw new ArgumentNullException(nameof(reference));
            }
            if (referenceType == null)
            {
                throw new ArgumentNullException(nameof(referenceType));
            }
            if (DependencyGraph == null)
            {
                return;
            }
            string file = IncrementalUtility.GetDependencyKey(currentFileModel.OriginalFileAndType);
            DependencyGraph.ReportReference(new ReferenceItem(new DependencyItemSourceInfo(referenceType, reference), file, file));
        }

        public bool HasMetadataValidation => Validators.Count > 0;

        public string MarkdownServiceName => MarkdownService.Name;

        public void ValidateInputMetadata(string sourceFile, ImmutableDictionary<string, object> metadata)
        {
            foreach (var v in Validators)
            {
                lock (v)
                {
                    v.Validate(sourceFile, metadata);
                }
            }
        }

        public void LogDiagnostic(string message, string file, string line)
        {
            Logger.LogDiagnostic(message, file: file, line: line);
        }

        public void LogVerbose(string message, string file, string line)
        {
            Logger.LogVerbose(message, file: file, line: line);
        }

        public void LogInfo(string message, string file, string line)
        {
            Logger.LogInfo(message, file: file, line: line);
        }

        public void LogWarning(string message, string file, string line)
        {
            Logger.LogWarning(message, file: file, line: line);
        }

        public void LogError(string message, string file, string line)
        {
            Logger.LogError(message, file: file, line: line);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            foreach (var m in Models)
            {
                m.FileOrBaseDirChanged -= HandleFileOrBaseDirChanged;
                m.UidsChanged -= HandleUidsChanged;
            }
        }

        #endregion

        public void Reload(IEnumerable<FileModel> models)
        {
            lock (_syncRoot)
            {
                LoadCore(models);
            }
        }

        #region Incremental Build

        public void RegisterDependencyType()
        {
            if (DependencyGraph == null)
            {
                return;
            }
            BuildPhaseUtility.RunBuildSteps(
                Processor.BuildSteps,
                buildStep =>
                {
                    if (buildStep is ISupportIncrementalBuildStep)
                    {
                        Logger.LogVerbose($"Processor {Processor.Name}, step {buildStep.Name}: Registering DependencyType...");
                        using (new LoggerPhaseScope(buildStep.Name, LogLevel.Diagnostic))
                        {
                            var types = (buildStep as ISupportIncrementalBuildStep).GetDependencyTypesToRegister();
                            if (types == null)
                            {
                                return;
                            }
                            DependencyGraph.RegisterDependencyType(types);
                        }
                    }
                });
        }

        public void ReloadModelsPerIncrementalChanges(IncrementalBuildContext incrementalContext, IEnumerable<string> changes, BuildPhase loadedAt)
        {
            if (changes == null)
            {
                return;
            }
            ReloadUnloadedModelsPerCondition(
                incrementalContext,
                loadedAt,
                f =>
                {
                    var key = ((RelativePath)f).GetPathFromWorkingFolder().ToString();
                    return changes.Contains(key);
                });
        }

        public void ReloadUnloadedModels(IncrementalBuildContext incrementalContext, BuildPhase loadedAt)
        {
            var mi = incrementalContext.GetModelLoadInfo(this);
            ReloadUnloadedModelsPerCondition(incrementalContext, loadedAt, f => mi[f] == null);
        }

        public void SaveIntermediateModel(IncrementalBuildContext incrementalContext)
        {
            if (!ShouldTraceIncrementalInfo)
            {
                return;
            }
            var processor = (ISupportIncrementalDocumentProcessor)Processor;
            var mi = incrementalContext.GetModelLoadInfo(this);
            var lmm = incrementalContext.GetLastIntermediateModelManifest(this);
            var cmm = incrementalContext.GetCurrentIntermediateModelManifest(this);

            Parallel.ForEach(mi, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, pair =>
            {
                IncrementalUtility.RetryIO(() =>
                {
                    var items = new List<ModelManifestItem>();
                    if (pair.Value == null)
                    {
                        if (lmm == null)
                        {
                            throw new BuildCacheException($"Full build hasn't loaded model {pair.Key}");
                        }
                        if (!lmm.Models.TryGetValue(pair.Key, out List<ModelManifestItem> lfn))
                        {
                            throw new BuildCacheException($"Last build hasn't loaded model {pair.Key}");
                        }

                        if (FilePathComparerWithEnvironmentVariable.OSPlatformSensitiveRelativePathComparer.Equals(
                            incrementalContext.BaseDir,
                            incrementalContext.LastBaseDir))
                        {
                            items.AddRange(lfn);
                        }
                        else
                        {
                            foreach (var item in lfn)
                            {
                                // use copy rather than move because if the build failed, the intermediate files of last successful build shouldn't be corrupted.
                                string fileName = IncrementalUtility.GetRandomEntry(incrementalContext.BaseDir);
                                File.Copy(
                                    Path.Combine(Environment.ExpandEnvironmentVariables(incrementalContext.LastBaseDir), item.FilePath),
                                    Path.Combine(Environment.ExpandEnvironmentVariables(incrementalContext.BaseDir), fileName));
                                items.Add(new ModelManifestItem() { SourceFilePath = item.SourceFilePath, FilePath = fileName });
                            }
                        }
                    }
                    else
                    {
                        var models = Models.Where(m => m.OriginalFileAndType.File == pair.Key).ToList();
                        foreach (var model in models)
                        {
                            string fileName = IncrementalUtility.GetRandomEntry(incrementalContext.BaseDir);
                            using (var stream = File.Create(
                            Path.Combine(
                                Environment.ExpandEnvironmentVariables(incrementalContext.BaseDir),
                                fileName)))
                            {
                                processor.SaveIntermediateModel(model, stream);
                            }
                            items.Add(new ModelManifestItem() { SourceFilePath = model.FileAndType.File, FilePath = fileName });
                        }
                    }
                    lock (cmm)
                    {
                        cmm.Models.Add(pair.Key, items);
                    }
                });
            });
        }

        public IEnumerable<FileModel> LoadIntermediateModel(IncrementalBuildContext incrementalContext, string fileName)
        {
            if (!CanIncrementalBuild)
            {
                yield break;
            }
            var processor = (ISupportIncrementalDocumentProcessor)Processor;
            var cmm = incrementalContext.GetCurrentIntermediateModelManifest(this);
            if (!cmm.Models.TryGetValue(fileName, out List<ModelManifestItem> cfn))
            {
                throw new BuildCacheException($"Last build hasn't loaded model {fileName}");
            }
            foreach (var item in cfn)
            {
                using (var stream = File.OpenRead(
                Path.Combine(Environment.ExpandEnvironmentVariables(incrementalContext.BaseDir), item.FilePath)))
                {
                    yield return processor.LoadIntermediateModel(stream);
                }
            }
        }

        public List<string> GetUnloadedModelFiles(IncrementalBuildContext incrementalContext)
        {
            if (!CanIncrementalBuild)
            {
                return new List<string>();
            }
            return (from pair in incrementalContext.GetModelLoadInfo(this)
                    where pair.Value == null
                    select pair.Key).ToList();
        }

        #endregion

        #region Private Methods

        private void LoadCore(IEnumerable<FileModel> models)
        {
            EventHandler fileOrBaseDirChangedHandler = HandleFileOrBaseDirChanged;
            EventHandler<PropertyChangedEventArgs<ImmutableArray<UidDefinition>>> uidsChangedHandler = HandleUidsChanged;
            EventHandler contentAccessedHandler = null;
            if (_lru != null)
            {
                contentAccessedHandler = ContentAccessedHandler;
            }
            if (Models != null)
            {
                foreach (var m in Models)
                {
                    m.FileOrBaseDirChanged -= fileOrBaseDirChangedHandler;
                    m.UidsChanged -= uidsChangedHandler;
                    m.ContentAccessed -= contentAccessedHandler;
                }
            }
            Models = models.ToImmutableList();
            _uidIndex.Clear();
            FileMap.Clear();
            foreach (var m in Models)
            {
                m.FileOrBaseDirChanged += fileOrBaseDirChangedHandler;
                m.UidsChanged += uidsChangedHandler;
                m.ContentAccessed += contentAccessedHandler;
                foreach (var uid in m.Uids.Select(s => s.Name).Distinct())
                {
                    if (!_uidIndex.TryGetValue(uid, out List<FileModel> list))
                    {
                        list = new List<FileModel>();
                        _uidIndex.Add(uid, list);
                    }
                    list.Add(m);
                }
                if (m.Type != DocumentType.Overwrite)
                {
                    FileMap[m.FileAndType] = m.FileAndType;
                }
            }
        }

        private void ReloadUnloadedModelsPerCondition(IncrementalBuildContext incrementalContext, BuildPhase phase, Func<string, bool> condition)
        {
            if (!CanIncrementalBuild)
            {
                return;
            }
            var mi = incrementalContext.GetModelLoadInfo(this);
            var toLoadList = (from f in mi.Keys
                              where condition(f)
                              from m in LoadIntermediateModel(incrementalContext, f)
                              select m).ToList();
            if (toLoadList.Count > 0)
            {
                Reload(Models.Concat(toLoadList));
                incrementalContext.ReportModelLoadInfo(this, toLoadList.Select(t => t.FileAndType.File), phase);
            }
        }

        private void HandleUidsChanged(object sender, PropertyChangedEventArgs<ImmutableArray<UidDefinition>> e)
        {
            var m = sender as FileModel;
            if (m == null)
            {
                return;
            }
            lock (_syncRoot)
            {
                var common = e.Original.Select(s => s.Name).Intersect(e.Current.Select(s => s.Name)).ToList();
                foreach (var added in e.Current.Select(s => s.Name).Except(common))
                {
                    if (!_uidIndex.TryGetValue(added, out List<FileModel> list))
                    {
                        list = new List<FileModel>();
                        _uidIndex.Add(added, list);
                    }
                    list.Add(m);
                }
                foreach (var removed in e.Original.Select(s => s.Name).Except(common))
                {
                    if (_uidIndex.TryGetValue(removed, out List<FileModel> list))
                    {
                        list.Remove(m);
                        if (list.Count == 0)
                        {
                            _uidIndex.Remove(removed);
                        }
                    }
                }
            }
        }

        private void HandleFileOrBaseDirChanged(object sender, EventArgs e)
        {
            var m = sender as FileModel;
            if (m == null)
            {
                return;
            }
            lock (_syncRoot)
            {
                FileMap[m.OriginalFileAndType] = m.FileAndType;
            }
        }

        private void ContentAccessedHandler(object sender, EventArgs e)
        {
            _lru.Access((ModelWithCache)sender);
        }

        private static void OnLruRemoving(ModelWithCache m)
        {
            try
            {
                m.Serialize();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Unable to serialize model, details:{ex.ToString()}", file: m.File);
            }
        }

        private void ReportDependencyCore(DependencyItemSourceInfo from, DependencyItemSourceInfo to, DependencyItemSourceInfo reportedBy, string type)
        {
            DependencyGraph.ReportDependency(new DependencyItem(from, to, reportedBy, type));
        }

        #endregion
    }
}
