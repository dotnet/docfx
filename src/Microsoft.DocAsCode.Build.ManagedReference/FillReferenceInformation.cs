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
    public class FillReferenceInformation : BaseDocumentBuildStep, ICanTraceContextInfoBuildStep
    {
        private IEnumerable<SourceInfo> _lastContextInfo;
        private Dictionary<string, SourceInfo> _items = new Dictionary<string, SourceInfo>();

        public override string Name => nameof(FillReferenceInformation);

        public override int BuildOrder => 0x20;

        public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            ApplyLastContextInfo(host);
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

        #region ICanTraceContextInfoBuildStep Members

        public void LoadContext(Stream stream)
        {
            if (stream == null)
            {
                return;
            }

            using (var reader = new StreamReader(stream))
            {
                _lastContextInfo = JsonUtility.Deserialize<IEnumerable<SourceInfo>>(reader);
            }
        }

        public void SaveContext(Stream stream)
        {
            if (stream == null)
            {
                return;
            }
            using (var writer = new StreamWriter(stream))
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
                        FillContent(r, i);
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

        private void FillContent(ReferenceViewModel r, dynamic item)
        {
            if (item.Metadata != null)
            {
                foreach (var pair in item.Metadata)
                {
                    switch (pair.Key)
                    {

                        case Constants.ExtensionMemberPrefix.Spec:
                            break;
                        default:
                            r.Additional[pair.Key] = pair.Value;
                            break;
                    }
                }
            }

            r.Additional["summary"] = item.Summary;
            r.Additional["type"] = item.Type;
            r.Additional["syntax"] = item.Syntax;
            r.Additional["platform"] = item.Platform;
        }

        private void ApplyLastContextInfo(IHostService hs)
        {
            if (_lastContextInfo == null)
            {
                return;
            }

            var increInfo = hs.IncrementalInfos;
            if (increInfo == null)
            {
                return;
            }

            foreach (var c in _lastContextInfo)
            {
                FileIncrementalInfo info;
                if (increInfo.TryGetValue(c.File, out info) && info.IsIncremental)
                {
                    _items[c.Uid] = c;
                }
            }
        }

        private void TraceSourceInfo(PageViewModel model, string file)
        {
            foreach (var item in model.Items)
            {
                _items[item.Uid] = new SourceInfo
                {
                    Uid = item.Uid,
                    Summary = item.Summary,
                    Type = item.Type,
                    Syntax = item.Syntax,
                    Platform = item.Platform,
                    File = file,
                    Metadata = item.Metadata,
                };
            }
        }

        #endregion

        private class SourceInfo
        {
            public string Uid { get; set; }
            public string Summary { get; set; }
            public MemberType? Type { get; set; }
            public SyntaxDetailViewModel Syntax { get; set; }
            public List<string> Platform { get; set; }
            public string File { get; set; }
            public Dictionary<string, object> Metadata { get; set; }
        }
    }
}
