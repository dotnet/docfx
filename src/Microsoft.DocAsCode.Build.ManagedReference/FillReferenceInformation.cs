// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class FillReferenceInformation : BaseDocumentBuildStep, ISupportIncrementalBuildStep, ICanTraceContextInfo
    {
        private Dictionary<string, SourceInfo> _items = new Dictionary<string, SourceInfo>();

        public override string Name => nameof(FillReferenceInformation);

        public override int BuildOrder => 0x20;

        public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            if (models.Count > 0)
            {
                foreach (var model in models)
                {
                    if (model.Type != DocumentType.Article)
                    {
                        continue;
                    }
                    FillCore((PageViewModel)model.Content, host, model.OriginalFileAndType.File);
                }
            }
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion

        #region ICanTraceContextInfo Members

        public void LoadFromContext(TraceContext traceContext, StreamReader reader)
        {
            if (traceContext == null || traceContext.Phase != BuildPhase.Link)
            {
                return;
            }
            if (reader == null)
            {
                return;
            }

            using (reader)
            {
                var content = JsonUtility.Deserialize<IEnumerable<SourceInfo>>(reader);
                var increInfo = (from f in traceContext.AllSourceFileInfo
                                group f by f.SourceFile).ToDictionary(g => g.Key, g => g.First());
                foreach (var c in content)
                {
                    FileIncrementalInfo info;
                    if (increInfo.TryGetValue(c.File, out info) && info.IsIncremental)
                    {
                        _items[c.Item.Uid] = c;
                    }
                }
            }
        }

        public void SaveContext(TraceContext traceContext, StreamWriter writer)
        {
            if (traceContext == null || traceContext.Phase != BuildPhase.Link)
            {
                return;
            }
            if (writer == null)
            {
                return;
            }
            using (writer)
            {
                JsonUtility.Serialize(writer, _items.Values);
            }
        }

        #endregion

        #region Private methods

        private void FillCore(PageViewModel model, IHostService host, string file)
        {
            TraceSourceInfo(model, file);
            if (model.References == null || model.References.Count == 0)
            {
                return;
            }
            foreach (var r in model.References)
            {
                var m = host.LookupByUid(r.Uid).Find(x => x.Type == DocumentType.Article);
                if (m == null)
                {
                    SourceInfo i;
                    if (_items.TryGetValue(r.Uid, out i))
                    {
                        FillContent(r, i.Item);
                    }
                    continue;
                }
                var page = (PageViewModel)m.Content;
                var item = page.Items.Find(x => x.Uid == r.Uid);
                if (item == null)
                {
                    continue;
                }
                FillContent(r, item);
            }
        }

        private void FillContent(ReferenceViewModel r, ItemViewModel item)
        {
            r.Additional["summary"] = item.Summary;
            r.Additional["type"] = item.Type;
            r.Additional["syntax"] = item.Syntax;
            r.Additional["platform"] = item.Platform;
        }

        private void TraceSourceInfo(PageViewModel model, string file)
        {
            foreach (var item in model.Items)
            {
                _items[item.Uid] = new SourceInfo { Item = item, File = file };
            }
        }

        #endregion

        private class SourceInfo
        {
            public ItemViewModel Item { get; set; }

            public string File { get; set; }
        }
    }
}
