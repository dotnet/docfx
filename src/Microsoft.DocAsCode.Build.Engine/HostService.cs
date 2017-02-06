﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Web;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IHostService))]
    internal sealed class HostService : IHostService, IDisposable
    {
        #region Fields
        private static readonly char[] UriFragmentOrQueryString = new char[] { '#', '?' };
        private readonly object _syncRoot = new object();
        private readonly object _tocSyncRoot = new object();
        private readonly Dictionary<string, List<FileModel>> _uidIndex = new Dictionary<string, List<FileModel>>();
        private readonly LruList<ModelWithCache> _lru = Environment.Is64BitProcess ? null : LruList<ModelWithCache>.CreateSynchronized(0xC00, OnLruRemoving);
        #endregion

        #region Properties

        public TemplateProcessor Template { get; set; }

        public ImmutableList<FileModel> Models { get; private set; }

        public ImmutableDictionary<string, FileAndType> SourceFiles { get; set; }

        public Dictionary<FileAndType, FileAndType> FileMap { get; } = new Dictionary<FileAndType, FileAndType>();

        public IMarkdownService MarkdownService { get; set; }

        public ImmutableList<IInputMetadataValidator> Validators { get; set; }

        public DependencyGraph DependencyGraph { get; set; }

        public bool ShouldTraceIncrementalInfo { get; set; }

        public bool CanIncrementalBuild { get; set; }

        public ImmutableList<TreeItemRestructure> TableOfContentRestructions { get; set; }
        #endregion

        #region Constructors

        public HostService(string baseDir, IEnumerable<FileModel> models)
        {
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
                List<FileModel> result;
                if (_uidIndex.TryGetValue(uid, out result))
                {
                    return result.ToImmutableList();
                }
                return ImmutableList<FileModel>.Empty;
            }
        }

        public MarkupResult Markup(string markdown, FileAndType ft)
        {
            if (markdown == null)
            {
                throw new ArgumentNullException(nameof(markdown));
            }
            if (ft == null)
            {
                throw new ArgumentNullException(nameof(ft));
            }
            return MarkupCore(markdown, ft, false);
        }

        public MarkupResult Markup(string markdown, FileAndType ft, bool omitParse)
        {
            if (markdown == null)
            {
                throw new ArgumentNullException(nameof(markdown));
            }
            if (ft == null)
            {
                throw new ArgumentNullException(nameof(ft));
            }
            return MarkupCore(markdown, ft, omitParse);
        }

        private MarkupResult MarkupCore(string markdown, FileAndType ft, bool omitParse)
        {
            try
            {
                var mr = MarkdownService.Markup(markdown, ft.File);
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

        public MarkupResult Parse(MarkupResult markupResult, FileAndType ft)
        {
            if (markupResult == null)
            {
                throw new ArgumentNullException(nameof(markupResult));
            }
            if (ft == null)
            {
                throw new ArgumentNullException(nameof(ft));
            }
            return ParseCore(markupResult, ft);
        }

        private MarkupResult ParseCore(MarkupResult markupResult, FileAndType ft)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(markupResult.Html);
            var result = markupResult.Clone();

            var node = doc.DocumentNode.SelectSingleNode("//yamlheader");
            if (node != null)
            {
                using (var sr = new StringReader(StringHelper.HtmlDecode(node.InnerHtml)))
                {
                    result.YamlHeader = YamlUtility.Deserialize<Dictionary<string, object>>(sr).ToImmutableDictionary();
                }
                node.Remove();
            }

            result.FileLinkSources = GetFileLinkSource(ft, doc);
            result.LinkToFiles = result.FileLinkSources.Keys.ToImmutableArray();

            result.UidLinkSources = GetUidLinkSources(doc);
            result.LinkToUids = result.UidLinkSources.Keys.ToImmutableHashSet();

            if (result.Dependency.Length > 0)
            {
                result.Dependency =
                    (from d in result.Dependency
                     select
                        ((RelativePath)ft.File + (RelativePath)d)
                            .GetPathFromWorkingFolder()
                            .ToString()
                    ).ToImmutableArray();
            }
            using (var sw = new StringWriter())
            {
                doc.Save(sw);
                result.Html = sw.ToString();
            }
            return result;
        }

        private ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> GetFileLinkSource(FileAndType ft, HtmlDocument doc)
        {
            var fileLinkSources = new Dictionary<string, List<LinkSourceInfo>>();
            foreach (var pair in (from n in doc.DocumentNode.Descendants()
                                  where !string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                                  from attr in n.Attributes
                                  where string.Equals(attr.Name, "src", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase)
                                  where !string.IsNullOrWhiteSpace(attr.Value)
                                  select new { Node = n, Attr = attr }).ToList())
            {
                string anchor = null;
                var link = pair.Attr;
                string linkFile = link.Value;
                var index = linkFile.IndexOfAny(UriFragmentOrQueryString);
                if (index != -1)
                {
                    anchor = linkFile.Substring(index);
                    linkFile = linkFile.Remove(index);
                }
                if (RelativePath.IsRelativePath(linkFile))
                {
                    var path = (RelativePath)ft.File + (RelativePath)linkFile;
                    var file = path.GetPathFromWorkingFolder().UrlDecode();
                    if (SourceFiles.ContainsKey(file))
                    {
                        string anchorInHref;
                        if (!string.IsNullOrEmpty(anchor) &&
                            string.Equals(link.Name, "href", StringComparison.OrdinalIgnoreCase))
                        {
                            anchorInHref = anchor;
                        }
                        else
                        {
                            anchorInHref = null;
                        }

                        link.Value = file.UrlEncode().ToString() + anchorInHref;
                    }

                    List<LinkSourceInfo> sources;
                    if (!fileLinkSources.TryGetValue(file, out sources))
                    {
                        sources = new List<LinkSourceInfo>();
                        fileLinkSources[file] = sources;
                    }
                    sources.Add(new LinkSourceInfo
                    {
                        Target = file,
                        Anchor = anchor,
                        SourceFile = pair.Node.GetAttributeValue("sourceFile", null),
                        LineNumber = pair.Node.GetAttributeValue("sourceStartLineNumber", 0),
                    });
                }
            }
            return fileLinkSources.ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutableList());
        }

        private static ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> GetUidLinkSources(HtmlDocument doc)
        {
            var uidInXref =
                from n in doc.DocumentNode.Descendants()
                where string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                from attr in n.Attributes
                where string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.Name, "uid", StringComparison.OrdinalIgnoreCase)
                select Tuple.Create(n, attr.Value);
            var uidInHref =
                from n in doc.DocumentNode.Descendants()
                where !string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                from attr in n.Attributes
                where string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.Name, "uid", StringComparison.OrdinalIgnoreCase)
                where attr.Value.StartsWith("xref:", StringComparison.OrdinalIgnoreCase)
                select Tuple.Create(n, attr.Value.Substring("xref:".Length));
            return (from pair in uidInXref.Concat(uidInHref)
                    where !string.IsNullOrWhiteSpace(pair.Item2)
                    let queryIndex = pair.Item2.IndexOfAny(UriFragmentOrQueryString)
                    let targetUid = queryIndex == -1 ? pair.Item2 : pair.Item2.Remove(queryIndex)
                    select new LinkSourceInfo
                    {
                        Target = Uri.UnescapeDataString(targetUid),
                        SourceFile = pair.Item1.GetAttributeValue("sourceFile", null),
                        LineNumber = pair.Item1.GetAttributeValue("sourceStartLineNumber", 0),
                    } into lsi
                    group lsi by lsi.Target into g
                    select new KeyValuePair<string, ImmutableList<LinkSourceInfo>>(g.Key, g.ToImmutableList())).ToImmutableDictionary();
        }

        public void ReportDependencyTo(FileModel currentFileModel, string to, string type)
        {
            if (currentFileModel == null)
            {
                throw new ArgumentNullException(nameof(currentFileModel));
            }
            if (string.IsNullOrEmpty(to))
            {
                throw new ArgumentNullException(nameof(to));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (DependencyGraph == null)
            {
                return;
            }
            lock (DependencyGraph)
            {
                string fromKey = IncrementalUtility.GetDependencyKey(currentFileModel.OriginalFileAndType);
                string toKey = IncrementalUtility.GetDependencyKey(currentFileModel.OriginalFileAndType.ChangeFile((RelativePath)currentFileModel.OriginalFileAndType.File + (RelativePath)to));
                ReportDependencyCore(fromKey, toKey, fromKey, type);
            }
        }

        public void ReportDependencyFrom(FileModel currentFileModel, string from, string type)
        {
            if (currentFileModel == null)
            {
                throw new ArgumentNullException(nameof(currentFileModel));
            }
            if (string.IsNullOrEmpty(from))
            {
                throw new ArgumentNullException(nameof(from));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (DependencyGraph == null)
            {
                return;
            }
            lock (DependencyGraph)
            {
                string fromKey = IncrementalUtility.GetDependencyKey(currentFileModel.OriginalFileAndType.ChangeFile((RelativePath)currentFileModel.OriginalFileAndType.File + (RelativePath)from));
                string toKey = IncrementalUtility.GetDependencyKey(currentFileModel.OriginalFileAndType);
                ReportDependencyCore(fromKey, toKey, toKey, type);
            }
        }

        public bool HasMetadataValidation => Validators.Count > 0;

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

            foreach (var pair in mi)
            {
                IncrementalUtility.RetryIO(() =>
                {
                    string fileName = IncrementalUtility.GetRandomEntry(incrementalContext.BaseDir);
                    if (pair.Value == null)
                    {
                        if (lmm == null)
                        {
                            throw new BuildCacheException($"Full build hasn't loaded model {pair.Key}");
                        }
                        string lfn;
                        if (!lmm.Models.TryGetValue(pair.Key, out lfn))
                        {
                            throw new BuildCacheException($"Last build hasn't loaded model {pair.Key}");
                        }

                        // use copy rather than move because if the build failed, the intermediate files of last successful build shouldn't be corrupted.
                        File.Copy(Path.Combine(incrementalContext.LastBaseDir, lfn), Path.Combine(incrementalContext.BaseDir, fileName));
                    }
                    else
                    {
                        var key = RelativePath.NormalizedWorkingFolder + pair.Key;
                        var model = Models.Find(m => m.Key == key);
                        using (var stream = File.Create(Path.Combine(incrementalContext.BaseDir, fileName)))
                        {
                            processor.SaveIntermediateModel(model, stream);
                        }
                    }
                    cmm.Models.Add(pair.Key, fileName);
                });
            }
        }

        public FileModel LoadIntermediateModel(IncrementalBuildContext incrementalContext, string fileName)
        {
            if (!CanIncrementalBuild)
            {
                return null;
            }
            var processor = (ISupportIncrementalDocumentProcessor)Processor;
            var cmm = incrementalContext.GetCurrentIntermediateModelManifest(this);
            string cfn;
            if (!cmm.Models.TryGetValue(fileName, out cfn))
            {
                throw new BuildCacheException($"Last build hasn't loaded model {fileName}");
            }
            using (var stream = File.OpenRead(Path.Combine(incrementalContext.BaseDir, cfn)))
            {
                return processor.LoadIntermediateModel(stream);
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
            if (!Environment.Is64BitProcess)
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
                foreach (var uid in m.Uids)
                {
                    List<FileModel> list;
                    if (!_uidIndex.TryGetValue(uid.Name, out list))
                    {
                        list = new List<FileModel>();
                        _uidIndex.Add(uid.Name, list);
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
                              select LoadIntermediateModel(incrementalContext, f) into m
                              where m != null
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
                    List<FileModel> list;
                    if (!_uidIndex.TryGetValue(added, out list))
                    {
                        list = new List<FileModel>();
                        _uidIndex.Add(added, list);
                    }
                    list.Add(m);
                }
                foreach (var removed in e.Original.Select(s => s.Name).Except(common))
                {
                    List<FileModel> list;
                    if (_uidIndex.TryGetValue(removed, out list))
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

        private void ReportDependencyCore(string from, string to, string reportedBy, string type)
        {
            DependencyGraph.ReportDependency(new DependencyItem(from, to, reportedBy, type));
        }

        #endregion
    }
}
