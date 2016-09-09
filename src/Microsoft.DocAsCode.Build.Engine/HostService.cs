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
    using System.Web;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IHostService))]
    internal sealed class HostService : IHostService, IDisposable
    {
        #region Fields
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, List<FileModel>> _uidIndex = new Dictionary<string, List<FileModel>>();
        private readonly LruList<ModelWithCache> _lru = Environment.Is64BitProcess ? null : LruList<ModelWithCache>.CreateSynchronized(0xC00, OnLruRemoving);
        private readonly Dictionary<FileAndType, LoadPhase> _modelLoadInfo = new Dictionary<FileAndType, LoadPhase>();
        #endregion

        #region Properties

        public TemplateProcessor Template { get; set; }

        public ImmutableList<FileModel> Models { get; private set; }

        public ImmutableDictionary<string, FileAndType> SourceFiles { get; set; }

        public Dictionary<FileAndType, FileAndType> FileMap { get; } = new Dictionary<FileAndType, FileAndType>();

        public IMarkdownService MarkdownService { get; set; }

        public ImmutableList<IInputMetadataValidator> Validators { get; set; }

        public DependencyGraph DependencyGraph { get; set; }

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
                Logger.LogWarning($"Markup failed:{Environment.NewLine}  Markdown: {markdown}{Environment.NewLine}  Details:{ex.ToString()}");
                return new MarkupResult { Html = markdown };
            }
        }

        public string MarkupToHtml(string markdown, string file)
        {
            return MarkdownService.Markup(markdown, file).Html;
        }

        public MarkupResult ParseHtml(string html, FileAndType ft)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var result = new MarkupResult();

            var node = doc.DocumentNode.SelectSingleNode("//yamlheader");
            if (node != null)
            {
                using (var sr = new StringReader(StringHelper.HtmlDecode(node.InnerHtml)))
                {
                    result.YamlHeader = YamlUtility.Deserialize<Dictionary<string, object>>(sr).ToImmutableDictionary();
                }
                node.Remove();
            }
            var linkToFiles = new HashSet<string>();
            var fileLinkSources = new Dictionary<string, List<LinkSourceInfo>>();
            foreach (var pair in (from n in doc.DocumentNode.Descendants()
                                  where !string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                                  from attr in n.Attributes
                                  where string.Equals(attr.Name, "src", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase)
                                  where !string.IsNullOrWhiteSpace(attr.Value)
                                  select new { Node = n, Attr = attr }).ToList())
            {
                string linkFile;
                string anchor = null;
                var link = pair.Attr;
                if (PathUtility.IsRelativePath(link.Value))
                {
                    var index = link.Value.IndexOf('#');
                    if (index == -1)
                    {
                        linkFile = link.Value;
                    }
                    else if (index == 0)
                    {
                        continue;
                    }
                    else
                    {
                        linkFile = link.Value.Remove(index);
                        anchor = link.Value.Substring(index);
                    }
                    var path = (RelativePath)ft.File + (RelativePath)linkFile;
                    var file = path.GetPathFromWorkingFolder();
                    if (SourceFiles.ContainsKey(file))
                    {
                        link.Value = file;
                        if (!string.IsNullOrEmpty(anchor) &&
                            string.Equals(link.Name, "href", StringComparison.OrdinalIgnoreCase))
                        {
                            pair.Node.SetAttributeValue("anchor", anchor);
                        }
                    }
                    linkToFiles.Add(HttpUtility.UrlDecode(file));

                    List<LinkSourceInfo> sources;
                    if (!fileLinkSources.TryGetValue(file, out sources))
                    {
                        sources = new List<LinkSourceInfo>();
                        fileLinkSources[file] = sources;
                    }
                    sources.Add(new LinkSourceInfo
                    {
                        Target = file,
                        SourceFile = pair.Node.GetAttributeValue("sourceFile", null),
                        LineNumber = pair.Node.GetAttributeValue("sourceLineNumber", 0),
                    });
                }
            }
            result.LinkToFiles = linkToFiles.ToImmutableArray();
            result.FileLinkSources = fileLinkSources.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableList());

            result.UidLinkSources = (from n in doc.DocumentNode.Descendants()
                                     where string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                                     from attr in n.Attributes
                                     where string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.Name, "uid", StringComparison.OrdinalIgnoreCase)
                                     where !string.IsNullOrWhiteSpace(attr.Value)
                                     select new LinkSourceInfo
                                     {
                                         Target = attr.Value,
                                         SourceFile = n.GetAttributeValue("sourceFile", null),
                                         LineNumber = n.GetAttributeValue("sourceLineNumber", 0),
                                     } into lsi
                                     group lsi by lsi.Target into g
                                     select new KeyValuePair<string, ImmutableList<LinkSourceInfo>>(g.Key, g.ToImmutableList())).ToImmutableDictionary();
            result.LinkToUids = result.UidLinkSources.Keys.ToImmutableHashSet();

            using (var sw = new StringWriter())
            {
                doc.Save(sw);
                result.Html = sw.ToString();
            }
            return result;
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
            var linkToFiles = new HashSet<string>();
            var fileLinkSources = new Dictionary<string, List<LinkSourceInfo>>();
            foreach (var pair in (from n in doc.DocumentNode.Descendants()
                                  where !string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                                  from attr in n.Attributes
                                  where string.Equals(attr.Name, "src", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase)
                                  where !string.IsNullOrWhiteSpace(attr.Value)
                                  select new { Node = n, Attr = attr }).ToList())
            {
                string linkFile;
                string anchor = null;
                var link = pair.Attr;
                if (PathUtility.IsRelativePath(link.Value))
                {
                    var index = link.Value.IndexOf('#');
                    if (index == -1)
                    {
                        linkFile = link.Value;
                    }
                    else if (index == 0)
                    {
                        continue;
                    }
                    else
                    {
                        linkFile = link.Value.Remove(index);
                        anchor = link.Value.Substring(index);
                    }
                    var path = (RelativePath)ft.File + (RelativePath)linkFile;
                    var file = path.GetPathFromWorkingFolder();
                    if (SourceFiles.ContainsKey(file))
                    {
                        link.Value = file;
                        if (!string.IsNullOrEmpty(anchor) &&
                            string.Equals(link.Name, "href", StringComparison.OrdinalIgnoreCase))
                        {
                            pair.Node.SetAttributeValue("anchor", anchor);
                        }
                    }
                    linkToFiles.Add(HttpUtility.UrlDecode(file));

                    List<LinkSourceInfo> sources;
                    if (!fileLinkSources.TryGetValue(file, out sources))
                    {
                        sources = new List<LinkSourceInfo>();
                        fileLinkSources[file] = sources;
                    }
                    sources.Add(new LinkSourceInfo
                    {
                        Target = file,
                        SourceFile = pair.Node.GetAttributeValue("sourceFile", null),
                        LineNumber = pair.Node.GetAttributeValue("sourceLineNumber", 0),
                    });
                }
            }
            result.LinkToFiles = linkToFiles.ToImmutableArray();
            result.FileLinkSources = fileLinkSources.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableList());

            result.UidLinkSources = (from n in doc.DocumentNode.Descendants()
                                     where string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                                     from attr in n.Attributes
                                     where string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.Name, "uid", StringComparison.OrdinalIgnoreCase)
                                     where !string.IsNullOrWhiteSpace(attr.Value)
                                     select new LinkSourceInfo
                                     {
                                         Target = attr.Value,
                                         SourceFile = n.GetAttributeValue("sourceFile", null),
                                         LineNumber = n.GetAttributeValue("sourceLineNumber", 0),
                                     } into lsi
                                     group lsi by lsi.Target into g
                                     select new KeyValuePair<string, ImmutableList<LinkSourceInfo>>(g.Key, g.ToImmutableList())).ToImmutableDictionary();
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

        #region Model Load Info

        public void ReportModelLoadInfo(FileAndType file, LoadPhase phase)
        {
            _modelLoadInfo[file] = phase;
        }

        public void ReportModelLoadInfo(IEnumerable<FileAndType> files, LoadPhase phase)
        {
            foreach (var f in files)
            {
                ReportModelLoadInfo(f, phase);
            }
        }

        public void ReloadModelsPerIncrementalChanges(IEnumerable<string> changes, string cacheFolder, LoadPhase phase)
        {
            if (changes == null)
            {
                return;
            }
            ReloadUnloadedModelsPerCondition(
                cacheFolder,
                phase,
                f =>
                {
                    var key = ((RelativePath)f.File).GetPathFromWorkingFolder().ToString();
                    return changes.Contains(key);
                });
        }

        public void ReloadUnloadedModels(string cacheFolder, LoadPhase phase)
        {
            ReloadUnloadedModelsPerCondition(cacheFolder, phase, f => _modelLoadInfo[f] == LoadPhase.None);
        }

        private void ReloadUnloadedModelsPerCondition(string cacheFolder, LoadPhase phase, Func<FileAndType, bool> condition)
        {
            if (cacheFolder == null)
            {
                return;
            }
            var toLoadList = (from f in _modelLoadInfo.Keys
                              where condition(f)
                              select LoadIntermediateModel(Path.Combine(cacheFolder, Path.GetFileName(f.File))) into m
                              where m != null
                              select m).ToList();
            if (toLoadList.Count > 0)
            {
                Reload(Models.Concat(toLoadList));
                ReportModelLoadInfo(toLoadList.Select(t => t.FileAndType), phase);
            }
        }

        public void SaveIntermediateModel(string intermediateFolder, ModelManifest lmm, ModelManifest cmm)
        {
            var processor = Processor as ISupportIncrementalDocumentProcessor;
            if (processor == null)
            {
                return;
            }

            foreach (var pair in _modelLoadInfo)
            {
                var fileName = Path.GetFileName(pair.Key.File);
                if (pair.Value == LoadPhase.None)
                {
                    if (lmm == null)
                    {
                        throw new InvalidDataException($"Full build hasn't loaded model {pair.Key.FullPath}");
                    }
                    File.Copy(Path.Combine(intermediateFolder, lmm.BaseDir, fileName), Path.Combine(intermediateFolder, cmm.BaseDir, fileName));
                }
                else
                {
                    using (var stream = new FileStream(Path.Combine(intermediateFolder, cmm.BaseDir, fileName), FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        // processor.SaveIntermediateModel(f, stream);
                    }
                }
                cmm.Models.Add(fileName);
            }
        }

        public FileModel LoadIntermediateModel(string fileName)
        {
            var processor = Processor as ISupportIncrementalDocumentProcessor;
            if (processor == null)
            {
                return null;
            }
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                // return processor.LoadIntermediateModel(stream);
                return null;
            }
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

    internal enum LoadPhase
    {
        None,
        PreBuild,
        PostBuild,
        PostPostBuild,
    }
}
