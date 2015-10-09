// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.IO;

    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility.EntityMergers;

    [Export(typeof(IDocumentProcessor))]
    public class ManagedReferenceDocumentProcessor : IDocumentProcessor
    {
        private readonly ReflectionEntityMerger Merger = new ReflectionEntityMerger();

        public ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    if (".csyml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase) ||
                        ".csyaml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                    {
                        return ProcessingPriority.Normal;
                    }
                    break;
                case DocumentType.Override:
                    if (".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                    {
                        return ProcessingPriority.Normal;
                    }
                    break;
                default:
                    break;
            }
            return ProcessingPriority.NotSupportted;
        }

        public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    var page = YamlUtility.Deserialize<PageViewModel>(Path.Combine(file.BaseDir, file.File));
                    if (page.Metadata == null)
                    {
                        page.Metadata = metadata.ToDictionary(p => p.Key, p => p.Value);
                    }
                    else
                    {
                        foreach (var item in metadata)
                        {
                            if (page.Metadata.ContainsKey(item.Key))
                            {
                                page.Metadata[item.Key] = item.Value;
                            }
                        }
                    }
                    var result = new FileModel(file, page, serializer: YamlFormatter<PageViewModel>.Instance)
                    {
                        Uids = (from item in page.Items select item.Uid).ToImmutableArray(),
                    };
                    result.Properties.LinkToFiles = new HashSet<string>();
                    result.Properties.LinkToUids = new HashSet<string>();
                    return result;
                case DocumentType.Override:
                    var overrides = MarkdownReader.ReadMarkdownAsOverride(file.BaseDir, file.File);
                    return new FileModel(file, overrides)
                    {
                        Uids = (from item in overrides
                                select item.Uid).ToImmutableArray(),
                    };
                default:
                    throw new NotSupportedException();
            }
        }

        public SaveResult Save(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }
            YamlUtility.Serialize(Path.Combine(model.BaseDir, model.File), model.Content);
            return new SaveResult
            {
                DocumentType = "ManagedReference",
                ModelFile = model.File,
                LinkToFiles = ((HashSet<string>)model.Properties.LinkToFiles).ToImmutableArray(),
                LinkToUids = ((HashSet<string>)model.Properties.LinkToUids).ToImmutableArray(), // todo : more uid link
            };
        }

        public IEnumerable<FileModel> Prebuild(ImmutableArray<FileModel> models, IHostService host)
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
                    break;
                case DocumentType.Override:
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        public IEnumerable<FileModel> Postbuild(ImmutableArray<FileModel> models, IHostService host)
        {
            if (models.Length > 0)
            {
                ApplyOverrides(models, host);
            }
            return models;
        }

        #region Private methods

        private void ApplyOverrides(ImmutableArray<FileModel> models, IHostService host)
        {
            foreach (var uid in host.GetAllUids())
            {
                var ms = host.LookupByUid(uid);
                var od = ms.SingleOrDefault(m => m.Type == DocumentType.Override);
                if (od != null)
                {
                    var ovm = ((List<ItemViewModel>)od.Content).Single(vm => vm.Uid == uid);
                    foreach (var item in from m in ms
                                         where m.Type == DocumentType.Article
                                         from item in ((PageViewModel)m.Content).Items
                                         where item.Uid == uid
                                         select item)
                    {
                        var vm = item;
                        // todo : fix file path
                        Merger.Merge(ref vm, ovm);
                    }
                }
            }
        }

        private void BuildItem(IHostService host, ItemViewModel item, FileModel model)
        {
            item.Summary = Markup(host, item.Summary, model);
            item.Remarks = Markup(host, item.Remarks, model);
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
