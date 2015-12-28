// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility.EntityMergers;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverrideDocument : BaseDocumentBuildStep
    {
        private readonly ReflectionEntityMerger Merger = new ReflectionEntityMerger();

        public override string Name => nameof(ApplyOverrideDocument);

        public override int BuildOrder => 0x10;

        public override IEnumerable<FileModel> Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            if (models.Count > 0)
            {
                ApplyOverrides(models, host);
            }
            return models;
        }

        #region Private methods

        private void ApplyOverrides(ImmutableList<FileModel> models, IHostService host)
        {
            foreach (var uid in host.GetAllUids())
            {
                var ms = host.LookupByUid(uid);
                var od = ms.SingleOrDefault(m => m.Type == DocumentType.Override);
                if (od != null)
                {
                    var ovm = ((List<ItemViewModel>)od.Content).Single(vm => vm.Uid == uid);
                    foreach (
                        var pair in
                            from model in ms
                            where model.Type == DocumentType.Article
                            from item in ((PageViewModel)model.Content).Items
                            where item.Uid == uid
                            select new { model, item })
                    {
                        var vm = pair.item;
                        // todo : fix file path
                        Merger.Merge(ref vm, ovm);
                        ((HashSet<string>)pair.model.Properties.LinkToUids).UnionWith((HashSet<string>)od.Properties.LinkToUids);
                        ((HashSet<string>)pair.model.Properties.LinkToFiles).UnionWith((HashSet<string>)od.Properties.LinkToFiles);
                    }
                }
            }
        }

        #endregion
    }
}
