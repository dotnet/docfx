// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System.Collections.Immutable;
    using System.Composition;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.UniversalReference;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(UniversalReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class FillReferenceInformation : BaseDocumentBuildStep
    {
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

        #region Private methods

        private void FillCore(PageViewModel model, IHostService host, string file)
        {
            if (model.References == null || model.References.Count == 0)
            {
                return;
            }
            foreach (var r in model.References)
            {
                var m = host.LookupByUid(r.Uid).Find(x => x.Type == DocumentType.Article);
                if (m == null)
                {
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

            r.Additional[Constants.PropertyName.Summary] = item.Summary;
            r.Additional[Constants.PropertyName.Type] = item.Type;
            r.Additional[Constants.PropertyName.Syntax] = item.Syntax;
            r.Additional[Constants.PropertyName.Platform] = item.Platform;
        }
        #endregion
    }
}
