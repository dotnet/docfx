// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Web;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IHostService))]
    internal sealed class HostService : IHostService, IDisposable
    {
        private readonly Dictionary<string, List<FileModel>> _uidIndex = new Dictionary<string, List<FileModel>>();
        private readonly LruList<FileModel> _lru = LruList<FileModel>.Create(0xC00, OnLruRemoving);

        public ImmutableList<FileModel> Models { get; private set; }

        public ImmutableDictionary<string, FileAndType> SourceFiles { get; set; }

        public Dictionary<FileAndType, FileAndType> FileMap { get; } = new Dictionary<FileAndType, FileAndType>();

        public HostService(IEnumerable<FileModel> models)
        {
            LoadCore(models);
        }

        #region IHostService Members

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
            return _uidIndex.Keys.ToImmutableHashSet();
        }

        public ImmutableList<FileModel> LookupByUid(string uid)
        {
            List<FileModel> result;
            if (_uidIndex.TryGetValue(uid, out result))
            {
                return result.ToImmutableList();
            }
            return ImmutableList<FileModel>.Empty;
        }

        public MarkupResult Markup(string markdown, FileAndType ft)
        {
            try
            {
                return MarkupCore(markdown, ft);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Fail("Markup failed!");
                Logger.LogWarning($"Markup failed:{Environment.NewLine}  Markdown: {markdown}{Environment.NewLine}  Details:{ex.ToString()}");
                return new MarkupResult { Html = markdown };
            }
        }

        public MarkupResult MarkupCore(string markdown, FileAndType ft)
        {
            var html = DocfxFlavoredMarked.Markup(markdown, Path.Combine(ft.BaseDir, ft.File));
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
            foreach (var link in from n in doc.DocumentNode.Descendants()
                                 where !string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                                 from attr in n.Attributes
                                 where string.Equals(attr.Name, "src", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase)
                                 where !string.IsNullOrWhiteSpace(attr.Value)
                                 select attr)
            {
                string linkFile;
                string anchor = null;
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
                    if (path.ParentDirectoryCount > 0)
                    {
                        Logger.LogError($"Cannot refer path: \"{path}\" out of project.", file: ft.File);
                        throw new DocumentException($"Cannot refer path \"{path}\" out of project in file \"{ft.File}\".");
                    }
                    var file = path.GetPathFromWorkingFolder();
                    link.Value = file + anchor;
                    linkToFiles.Add(HttpUtility.UrlDecode(file));
                }
            }
            result.LinkToFiles = linkToFiles.ToImmutableArray();
            result.LinkToUids = (from n in doc.DocumentNode.Descendants()
                                 where string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                                 from attr in n.Attributes
                                 where string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase)
                                 where !string.IsNullOrWhiteSpace(attr.Value)
                                 select attr.Value).ToImmutableArray();
            using (var sw = new StringWriter())
            {
                doc.Save(sw);
                result.Html = sw.ToString();
            }
            return result;
        }

        public void LogVerbose(string message, string file, string line)
        {
            Logger.LogVerbose(message, "Build Document - Plugin", file, line);
        }

        public void LogInfo(string message, string file, string line)
        {
            Logger.LogInfo(message, "Build Document - Plugin", file, line);
        }

        public void LogWarning(string message, string file, string line)
        {
            Logger.LogWarning(message, "Build Document - Plugin", file, line);
        }

        public void LogError(string message, string file, string line)
        {
            Logger.LogError(message, "Build Document - Plugin", file, line);
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
            LoadCore(models);
        }

        private void LoadCore(IEnumerable<FileModel> models)
        {
            EventHandler fileOrBaseDirChangedHandler = HandleFileOrBaseDirChanged;
            EventHandler<PropertyChangedEventArgs<ImmutableArray<string>>> uidsChangedHandler = HandleUidsChanged;
            EventHandler contentAccessedHandler = ContentAccessedHandler;
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
                    if (!_uidIndex.TryGetValue(uid, out list))
                    {
                        list = new List<FileModel>();
                        _uidIndex.Add(uid, list);
                    }
                    list.Add(m);
                }
                if (m.Type != DocumentType.Override)
                {
                    FileMap[m.FileAndType] = m.FileAndType;
                }
            }
        }

        private void HandleUidsChanged(object sender, PropertyChangedEventArgs<ImmutableArray<string>> e)
        {
            var m = sender as FileModel;
            if (m == null)
            {
                return;
            }
            var common = e.Original.Intersect(e.Current).ToList();
            foreach (var added in e.Current.Except(common))
            {
                List<FileModel> list;
                if (!_uidIndex.TryGetValue(added, out list))
                {
                    list = new List<FileModel>();
                    _uidIndex.Add(added, list);
                }
                list.Add(m);
            }
            foreach (var removed in e.Original.Except(common))
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

        private void HandleFileOrBaseDirChanged(object sender, EventArgs e)
        {
            var m = sender as FileModel;
            if (m == null)
            {
                return;
            }
            FileMap[m.OriginalFileAndType] = m.FileAndType;
        }

        private void ContentAccessedHandler(object sender, EventArgs e)
        {
            _lru.Access((FileModel)sender);
        }

        private static void OnLruRemoving(FileModel m)
        {
            m.Serialize();
        }
    }
}
