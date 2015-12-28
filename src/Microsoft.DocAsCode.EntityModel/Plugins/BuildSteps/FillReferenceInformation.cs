// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class FillReferenceInformation : BaseDocumentBuildStep
    {
        public override string Name => nameof(FillReferenceInformation);

        public override int BuildOrder => 0x20;

        public override IEnumerable<FileModel> Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            if (models.Count > 0)
            {
                foreach (var model in models)
                {
                    if (model.Type != DocumentType.Article)
                    {
                        continue;
                    }
                    FillCore((PageViewModel)model.Content, host);
                }
            }
            return models;
        }

        #region Private methods

        private void FillCore(PageViewModel model, IHostService host)
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
                r.Summary = item.Summary;
                r.Type = item.Type;
            }
        }

        #endregion
    }
}
