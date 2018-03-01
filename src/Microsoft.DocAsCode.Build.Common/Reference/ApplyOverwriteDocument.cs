// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Common.EntityMergers;

    using YamlDotNet.Core;

    public abstract class ApplyOverwriteDocument : BaseDocumentBuildStep
    {
        private readonly MergerFacade _merger;

        private readonly IModelAttributeHandler _handler;

        protected ApplyOverwriteDocument()
        {
            _merger = new MergerFacade(GetMerger());
            _handler = new CompositeModelAttributeHandler(
                new UniqueIdentityReferenceHandler(),
                new MarkdownContentHandler()
                );
        }

        protected virtual IMerger GetMerger()
        {
            return new DictionaryMerger(
                new KeyedListMerger(
                    new ReflectionEntityMerger()));
        }

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

        protected abstract void ApplyOverwrite(IHostService host, List<FileModel> overwrites, string uid, List<FileModel> articles);

        protected void ApplyOverwrite<T>(
            IHostService host,
            List<FileModel> overwrites,
            string uid,
            List<FileModel> articles,
            Func<FileModel, string, IHostService, IEnumerable<T>> getItemsFromOverwriteDocument,
            Func<FileModel, string, IHostService, IEnumerable<T>> getItemsToOverwrite)
            where T : class, IOverwriteDocumentViewModel
        {
            // Multiple UID in overwrite documents is allowed now
            var ovms = (from fm in overwrites
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

                foreach (var overwriteDocumentModel in overwrites.Select(overwrite => GetOverwriteDocumentModelsByUid(overwrite, uid)).SelectMany(o => o))
                {
                    pair.model.LinkToUids = pair.model.LinkToUids.Union(overwriteDocumentModel.LinkToUids);
                    pair.model.LinkToFiles = pair.model.LinkToFiles.Union(overwriteDocumentModel.LinkToFiles);
                    pair.model.FileLinkSources = pair.model.FileLinkSources.ToDictionary(i => i.Key, i => i.Value.ToList())
                        .Merge(overwriteDocumentModel.FileLinkSources.Select(i => new KeyValuePair<string, IEnumerable<LinkSourceInfo>>(i.Key, i.Value)))
                        .ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList());
                    pair.model.UidLinkSources = pair.model.UidLinkSources.ToDictionary(i => i.Key, i => i.Value.ToList())
                        .Merge(overwriteDocumentModel.UidLinkSources.Select(i => new KeyValuePair<string, IEnumerable<LinkSourceInfo>>(i.Key, i.Value)))
                        .ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList());
                }
            }
        }

        protected IEnumerable<T> Transform<T>(FileModel model, string uid, IHostService host) where T : class, IOverwriteDocumentViewModel
        {
            var overwrites = ((List<OverwriteDocumentModel>)model.Content).Where(s => s.Uid == uid);
            return overwrites.Select(s =>
            {
                try
                {
                    var placeholderContent = s.Conceptual;
                    s.Conceptual = null;
                    var item = s.ConvertTo<T>();
                    var context = new HandleModelAttributesContext
                    {
                        EnableContentPlaceholder = true,
                        Host = host,
                        PlaceholderContent = placeholderContent,
                        FileAndType = model.OriginalFileAndType,
                    };

                    _handler.Handle(item, context);
                    if (!context.ContainsPlaceholder)
                    {
                        item.Conceptual = placeholderContent;
                    }
                    return item;
                }
                catch (YamlException ye)
                {
                    throw new DocumentException($"Unable to deserialize YAML header from \"{s.Documentation.Path}\" Line {s.Documentation.StartLine} to TYPE {typeof(T).Name}: {ye.Message}", ye);
                }
            });
        }

        private static List<OverwriteDocumentModel> GetOverwriteDocumentModelsByUid(FileModel overwriteFileModel, string uid)
        {
            return ((IEnumerable<OverwriteDocumentModel>)overwriteFileModel.Content).Where(o => o.Uid == uid).ToList();
        }

        private T Merge<T>(T baseModel, T overrideModel, FileModel model) where T : class, IOverwriteDocumentViewModel
        {
            try
            {
                _merger.Merge(ref baseModel, overrideModel);
            }
            catch (Exception e)
            {
                throw new DocumentException($"Error merging overwrite document from {model.OriginalFileAndType}: {e.Message}", e);
            }

            return baseModel;
        }
    }
}
