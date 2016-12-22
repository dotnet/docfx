// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.ReferenceBase;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocumentForMref : ApplyOverwriteDocumentForReferenceBase<ItemViewModel>
    {
        public override string Name => nameof(ApplyOverwriteDocumentForMref);

        #region ApplyOverwriteDocumentForReferenceBase Members

        protected override IEnumerable<ItemViewModel> GetItemsFromOverwriteDocument(FileModel fileModel, string uid, IHostService host)
        {
            return OverwriteDocumentReader.Transform<ItemViewModel>(
                fileModel,
                uid,
                s => BuildManagedReferenceDocument.BuildItem(host, s, fileModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
        }

        protected override IEnumerable<ItemViewModel> GetItemsToOverwrite(FileModel fileModel, string uid, IHostService host)
        {
            return ((PageViewModel)fileModel.Content).Items.Where(s => s.Uid == uid);
        }

        #endregion
    }
}
