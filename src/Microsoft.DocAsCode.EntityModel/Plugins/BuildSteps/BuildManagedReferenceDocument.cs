// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Utility.EntityMergers;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildManagedReferenceDocument : IDocumentBuildStep
    {
        private readonly ReflectionEntityMerger Merger = new ReflectionEntityMerger();

        public string Name => nameof(BuildManagedReferenceDocument);

        public int BuildOrder => 0;

        public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            return models;
        }

        public void Build(FileModel model, IHostService host)
        {
            switch (model.Type)
            {
                case DocumentType.Article:
                    var page = (PageViewModel)model.Content;
                    foreach (var item in page.Items)
                    {
                        BuildItem(host, item, model);
                    }
                    foreach (var reference in page.References)
                    {
                        BuildReference(host, reference, model);
                    }

                    model.File = Path.ChangeExtension(model.File, ".json");
                    break;
                case DocumentType.Override:
                    foreach (var item in (List<ItemViewModel>)model.Content)
                    {
                        BuildItem(host, item, model);
                    }
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        public IEnumerable<FileModel> Postbuild(ImmutableList<FileModel> models, IHostService host)
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

        private void BuildItem(IHostService host, ItemViewModel item, FileModel model)
        {
            item.Summary = Markup(host, item.Summary, model);
            item.Remarks = Markup(host, item.Remarks, model);
            item.Conceptual = Markup(host, item.Conceptual, model);
            if (item.Syntax?.Return?.Description != null)
            {
                item.Syntax.Return.Description = Markup(host, item.Syntax?.Return?.Description, model);
            }
            var parameters = item.Syntax?.Parameters;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    parameter.Description = Markup(host, parameter.Description, model);
                }
            }
            if (item.Exceptions != null)
            {
                foreach (var exception in item.Exceptions)
                {
                    exception.Description = Markup(host, exception.Description, model);
                }
            }
        }

        private void BuildReference(IHostService host, ReferenceViewModel reference, FileModel model)
        {
            reference.Summary = Markup(host, reference.Summary, model);
        }

        private string Markup(IHostService host, string markdown, FileModel model)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }
            var mr = host.Markup(markdown, model.FileAndType);
            ((HashSet<string>)model.Properties.LinkToFiles).UnionWith(mr.LinkToFiles);
            ((HashSet<string>)model.Properties.LinkToUids).UnionWith(mr.LinkToUids);
            return mr.Html;
        }

        #endregion
    }
}
