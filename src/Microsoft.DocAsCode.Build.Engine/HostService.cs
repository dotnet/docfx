// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdigEngine;
    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IHostService))]
    internal sealed class HostService : IHostService, IDisposable
    {
        #region Fields
        private readonly object _syncRoot = new object();
        private readonly object _tocSyncRoot = new object();
        private readonly Dictionary<string, List<FileModel>> _uidIndex = new Dictionary<string, List<FileModel>>();
        #endregion

        #region Properties

        public IBuildParameters BuildParameters { get; }

        public TemplateProcessor Template { get; set; }

        public ImmutableList<FileModel> Models { get; private set; }

        public ImmutableDictionary<string, FileAndType> SourceFiles { get; set; }

        public ImmutableList<string> InvalidSourceFiles { get; set; }

        public Dictionary<FileAndType, FileAndType> FileMap { get; } = new Dictionary<FileAndType, FileAndType>();

        public IMarkdownService MarkdownService { get; set; }

        public ImmutableList<IInputMetadataValidator> Validators { get; set; }

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
                var mr = MarkdownService is MarkdigMarkdownService markdig
                    ? markdig.Markup(markdown, ft.File, enableValidation, ft.Type is DocumentType.Overwrite)
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
                Logger.LogError(message, code: ErrorCodes.Build.InvalidMarkdown);
                throw new DocumentException(message, ex);
            }
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

        public void LogSuggestion(string message, string file, string line)
        {
            Logger.LogSuggestion(message, file: file, line: line);
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

        #region Private Methods

        private void LoadCore(IEnumerable<FileModel> models)
        {
            EventHandler fileOrBaseDirChangedHandler = HandleFileOrBaseDirChanged;
            EventHandler<PropertyChangedEventArgs<ImmutableArray<UidDefinition>>> uidsChangedHandler = HandleUidsChanged;
            if (Models != null)
            {
                foreach (var m in Models)
                {
                    m.FileOrBaseDirChanged -= fileOrBaseDirChangedHandler;
                    m.UidsChanged -= uidsChangedHandler;
                }
            }
            Models = models.ToImmutableList();
            _uidIndex.Clear();
            FileMap.Clear();
            foreach (var m in Models)
            {
                m.FileOrBaseDirChanged += fileOrBaseDirChangedHandler;
                m.UidsChanged += uidsChangedHandler;
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

        private void HandleUidsChanged(object sender, PropertyChangedEventArgs<ImmutableArray<UidDefinition>> e)
        {
            if (!(sender is FileModel m))
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
            if (!(sender is FileModel m))
            {
                return;
            }
            lock (_syncRoot)
            {
                FileMap[m.OriginalFileAndType] = m.FileAndType;
            }
        }

        #endregion
    }
}
