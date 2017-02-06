﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using CommonConstants = Microsoft.DocAsCode.DataContracts.Common.Constants;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(JavaScriptReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocumentForJsRef : ApplyOverwriteDocument
    {
        public override string Name => nameof(ApplyOverwriteDocumentForJsRef);

        public override int BuildOrder => 0x10;

        public IEnumerable<ItemViewModel> GetItemsFromOverwriteDocument(FileModel fileModel, string uid, IHostService host)
        {
            return OverwriteDocumentReader.Transform<ItemViewModel>(
                fileModel,
                uid,
                s => BuildJavaScriptReferenceDocument.BuildItem(host, s, fileModel, content => content != null && content.Trim() == CommonConstants.ContentPlaceholder));
        }

        public IEnumerable<ItemViewModel> GetItemsToOverwrite(FileModel fileModel, string uid, IHostService host)
        {
            return ((PageViewModel)fileModel.Content).Items.Where(s => s.Uid == uid);
        }

        protected override void ApplyOverwrite(IHostService host, List<FileModel> overwrites, string uid, List<FileModel> articles)
        {
            ApplyOverwrite(host, overwrites, uid, articles, GetItemsFromOverwriteDocument, GetItemsToOverwrite);
        }
    }
}
