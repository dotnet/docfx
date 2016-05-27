// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocumentForMref : ApplyOverwriteDocument
    {
        public override string Name => nameof(ApplyOverwriteDocumentForMref);

        public override int BuildOrder => 0x10;

        public Func<FileModel, string, IHostService, IEnumerable<ItemViewModel>> GetItemsFromOverwriteDocument =
            (((fileModel, uid, host) =>
            {
                return OverwriteDocumentReader.Transform<ItemViewModel>(
                    fileModel,
                    uid,
                    s => BuildManagedReferenceDocument.BuildItem(host, s, fileModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
            }));

        public Func<FileModel, string, IHostService, IEnumerable<ItemViewModel>> GetItemsToOverwrite =
            (((fileModel, uid, host) =>
            {
                return ((PageViewModel)fileModel.Content).Items.Where(s => s.Uid == uid);
            }));

        protected override void ApplyOvewriteDocument(IHostService host, List<FileModel> od, string uid, List<FileModel> articles)
        {
            ApplyOvewriteDocument(host, od, uid, articles, GetItemsFromOverwriteDocument, GetItemsToOverwrite);
        }
    }
}
