// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocumentForMref : ApplyOverwriteDocument, ISupportIncrementalBuildStep
    {
        public override string Name => nameof(ApplyOverwriteDocumentForMref);

        public override int BuildOrder => 0x10;

        public IEnumerable<ItemViewModel> GetItemsFromOverwriteDocument(FileModel fileModel, string uid, IHostService host)
        {
            return Transform<ItemViewModel>(fileModel, uid, host);
        }

        public IEnumerable<ItemViewModel> GetItemsToOverwrite(FileModel fileModel, string uid, IHostService host)
        {
            return ((PageViewModel)fileModel.Content).Items.Where(s => s.Uid == uid);
        }

        protected override void ApplyOverwrite(IHostService host, List<FileModel> overwrites, string uid, List<FileModel> articles)
        {
            ApplyOverwrite(host, overwrites, uid, articles, GetItemsFromOverwriteDocument, GetItemsToOverwrite);
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion

    }
}
