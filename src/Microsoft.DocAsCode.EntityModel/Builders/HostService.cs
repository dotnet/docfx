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

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;
    using HtmlAgilityPack;

    [Export(typeof(IHostService))]
    internal sealed class HostService : IHostService, IDisposable
    {
        private readonly Dictionary<string, List<FileModel>> _uidIndex = new Dictionary<string, List<FileModel>>();
        private readonly RelativePath RootSymbol = (RelativePath)"~/";

        public ImmutableArray<FileModel> Models { get; private set; }

        public Dictionary<FileAndType, FileAndType> FileMap { get; } = new Dictionary<FileAndType, FileAndType>();

        public HostService(IEnumerable<FileModel> models)
        {
            LoadCore(models);
        }

        #region IHostService Members

        public ImmutableArray<FileModel> GetModels(DocumentType? type)
        {
            if (type == null)
            {
                return Models;
            }
            return (from m in Models where m.Type == type select m).ToImmutableArray();
        }

        public ImmutableHashSet<string> GetAllUids()
        {
            return _uidIndex.Keys.ToImmutableHashSet();
        }

        public ImmutableArray<FileModel> LookupByUid(string uid)
        {
            List<FileModel> result;
            if (_uidIndex.TryGetValue(uid, out result))
            {
                return result.ToImmutableArray();
            }
            return ImmutableArray<FileModel>.Empty;
        }

        public MarkupResult Markup(string markdown, FileAndType ft)
        {
            var html = DocfxFlavoredMarked.Markup(markdown, Path.Combine(ft.BaseDir, ft.File));
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            foreach (var link in from n in doc.DocumentNode.Descendants()
                                 where n.Name != "xref"
                                 from attr in n.Attributes
                                 where string.Equals(attr.Name, "src", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase)
                                 where !string.IsNullOrWhiteSpace(attr.Value)
                                 select attr)
            {
                Uri uri;
                if (Uri.TryCreate(link.Value, UriKind.Relative, out uri))
                {
                    var path = (RelativePath)ft.File + (RelativePath)link.Value;
                    if (path.ParentDirectoryCount > 0)
                    {
                        throw new InvalidOperationException($"Cannot refer out of project. Path: {path}");
                    }
                    link.Value = RootSymbol + path;
                }
            }
            var result = new MarkupResult();
            var node = doc.DocumentNode.SelectSingleNode("//yamlheader");
            if (node != null)
            {
                using (var sr = new StringReader(node.InnerHtml))
                {
                    result.YamlHeader = YamlUtility.Deserialize<Dictionary<string, object>>(sr);
                }
                node.Remove();
            }
            using (var sw = new StringWriter())
            {
                doc.Save(sw);
                result.Html = sw.ToString();
            }
            return result;
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
            if (Models != null)
            {
                foreach (var m in Models)
                {
                    m.FileOrBaseDirChanged -= fileOrBaseDirChangedHandler;
                    m.UidsChanged -= uidsChangedHandler;
                }
            }
            Models = models.ToImmutableArray();
            _uidIndex.Clear();
            FileMap.Clear();
            foreach (var m in Models)
            {
                m.FileOrBaseDirChanged += fileOrBaseDirChangedHandler;
                m.UidsChanged += uidsChangedHandler;
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
    }
}
