// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Collections.Generic;
    using System.Composition;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocumentForMref : ApplyOverwriteDocument<ItemViewModel>
    {
        public override string Name => nameof(ApplyOverwriteDocumentForMref);

        public override int BuildOrder => 0x10;

        protected override IEnumerable<ItemViewModel> GetItemsFromOverwriteDocument(FileModel fileModel, IHostService host)
        {
            var item = OverwriteDocumentReader.Transform<ItemViewModel>(
                fileModel,
                s => BuildManagedReferenceDocument.BuildItem(host, s, fileModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
            fileModel.Content = item;
            return item;
        }

        protected override IEnumerable<ItemViewModel> GetItemsToOverwrite(FileModel model, IHostService host)
        {
            return ((PageViewModel)model.Content).Items;
        }
    }
}
