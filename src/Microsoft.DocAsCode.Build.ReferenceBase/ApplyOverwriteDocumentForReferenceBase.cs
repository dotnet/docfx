// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ReferenceBase
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    /// <summary>
    /// Base document build step for reference to apply overwrite document.
    /// </summary>
    /// <typeparam name="T">The type of model that overwrite document is applied to.</typeparam>
    public abstract class ApplyOverwriteDocumentForReferenceBase<T> : ApplyOverwriteDocument
            where T : class, IOverwriteDocumentViewModel
    {
        public override int BuildOrder => 0x10;

        protected override void ApplyOverwrite(IHostService host, List<FileModel> od, string uid, List<FileModel> articles)
        {
            ApplyOverwrite(host, od, uid, articles, GetItemsFromOverwriteDocument, GetItemsToOverwrite);
        }

        protected abstract IEnumerable<T> GetItemsFromOverwriteDocument(FileModel fileModel, string uid, IHostService host);

        protected abstract IEnumerable<T> GetItemsToOverwrite(FileModel filemodel, string uid, IHostService host);
    }
}
