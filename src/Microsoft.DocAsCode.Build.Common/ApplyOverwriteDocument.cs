// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Common.EntityMergers;

    public abstract class ApplyOverwriteDocument : BaseDocumentBuildStep
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

        protected virtual void ApplyOverwrites(ImmutableList<FileModel> models, IHostService host)
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

                ApplyOverwrite(host, od, uid, articles);
            }
        }

        protected abstract void ApplyOverwrite(IHostService host, List<FileModel> od, string uid, List<FileModel> articles);

        protected void ApplyOverwrite<T>(
            IHostService host,
            List<FileModel> od,
            string uid,
            List<FileModel> articles,
            Func<FileModel, string, IHostService, IEnumerable<T>> getItemsFromOverwriteDocument,
            Func<FileModel, string, IHostService, IEnumerable<T>> getItemsToOverwrite)
            where T : class, IOverwriteDocumentViewModel
        {
            // Multiple UID in overwrite documents is allowed now
            var ovms =
                (from fm in od.Distinct()
                 from content in getItemsFromOverwriteDocument(fm, uid, host)
                 select new
                 {
                     model = content,
                     fileModel = fm
                 }).ToList();

            if (ovms.Count == 0)
            {
                return;
            }

            // 1. merge all the overwrite document into one overwrite view model
            var ovm = ovms.Skip(1).Aggregate(ovms[0].model, (accum, item) => Merge(accum, item.model, item.fileModel));

            // 2. apply the view model to articles matching the uid
            foreach (
                var pair in
                    from model in articles
                    from item in getItemsToOverwrite(model, uid, host)
                    select new { model, item })
            {
                var vm = pair.item;
                Merge(vm, ovm, ovms[0].fileModel);
                pair.model.LinkToUids = pair.model.LinkToUids.Union(od[0].LinkToUids);
                pair.model.LinkToFiles = pair.model.LinkToFiles.Union(od[0].LinkToFiles);
            }
        }

        private T Merge<T>(T baseModel, T overrideModel, FileModel model) where T : class, IOverwriteDocumentViewModel
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
    }
}
