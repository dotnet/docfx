// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility.EntityMergers;

    public abstract class ApplyOverwriteDocument<T> : BaseDocumentBuildStep where T : class, IOverwriteDocumentViewModel
    {
        private readonly MergerFacade Merger = new MergerFacade(
                new DictionaryMerger(
                    new KeyedListMerger(
                        new ReflectionEntityMerger())));

        public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            if (models.Count > 0)
            {
                ApplyOverwrites(models, host);
            }
        }

        protected abstract IEnumerable<T> GetItemsFromOverwriteDocument(FileModel fileModel, IHostService host);

        protected abstract IEnumerable<T> GetItemsToOverwrite(FileModel fileModel, IHostService host);

        #region Private methods

        private void ApplyOverwrites(ImmutableList<FileModel> models, IHostService host)
        {
            foreach (var uid in host.GetAllUids())
            {
                var ms = host.LookupByUid(uid);
                var od = ms.Where(m => m.Type == DocumentType.Overwrite).ToList();
                var articles = ms.Except(od).ToList();
                if (articles.Count == 0 || od.Count == 0)
                {
                    continue;
                }

                // Multiple UID in overwrite documents is allowed now
                var ovms =
                    (from fm in od.Distinct()
                     from content in GetItemsFromOverwriteDocument(fm, host)
                     where content.Uid == uid
                     select new
                     {
                         model = content,
                         fileModel = fm
                     }).ToList();

                if (ovms.Count == 0)
                {
                    continue;
                }

                // 1. merge all the overwrite document into one overwrite view model
                var ovm = ovms.Skip(1).Aggregate(ovms[0].model, (accum, item) => Merge(accum, item.model, item.fileModel));
                
                // 2. apply the view model to articles matching the uid
                foreach (
                    var pair in
                        from model in articles
                        from item in GetItemsToOverwrite(model, host)
                        where item.Uid == uid
                        select new { model, item })
                {
                    var vm = pair.item;
                    Merge(vm, ovm, ovms[0].fileModel);
                    ((HashSet<string>)pair.model.Properties.LinkToUids).UnionWith((HashSet<string>)od[0].Properties.LinkToUids);
                    ((HashSet<string>)pair.model.Properties.LinkToFiles).UnionWith((HashSet<string>)od[0].Properties.LinkToFiles);
                }
            }
        }

        private T Merge(T baseModel, T overrideModel, FileModel model)
        {
            try
            {
                Merger.Merge(ref baseModel, overrideModel);
            }
            catch (Exception e)
            {
                throw new DocumentException($"Error merging overwrite document from {model.OriginalFileAndType}: {e.Message}", e);
            }

            return baseModel;
        }

        #endregion
    }
}
