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

    [Export(typeof(IDocumentProcessor))]
    public class ManagedReferenceDocumentProcessor : IDocumentProcessor
    {
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

        public FileModel Load(FileAndType file)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    var page = YamlUtility.Deserialize<PageViewModel>(Path.Combine(file.BaseDir, file.File));
                    return new FileModel(file, page, serializer: YamlFormatter<PageViewModel>.Instance)
                    {
                        Uids = (from item in page.Items select item.Uid).ToImmutableArray(),
                    };
                case DocumentType.Override:
                    var overrides = MarkdownReader.ReadMarkdownAsOverride(file.BaseDir, file.File);
                    return new FileModel(file, overrides)
                    {
                        // todo : strong type
                        Uids = (from Dictionary<string, object> item in (List<Dictionary<string, object>>)overrides["items"]
                                select (string)item["uid"]).ToImmutableArray(),
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
                        BuildItem(host, item, model.FileAndType);
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
            // todo : merge
            return models;
        }

        private void BuildItem(IHostService host, ItemViewModel item, FileAndType ft)
        {
            item.Summary = Markup(host, item.Summary, ft);
            item.Remarks = Markup(host, item.Remarks, ft);
            if (item.Syntax?.Return?.Description != null)
            {
                item.Syntax.Return.Description = Markup(host, item.Syntax?.Return?.Description, ft);
            }
            var parameters = item.Syntax?.Parameters;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    parameter.Description = Markup(host, parameter.Description, ft);
                }
            }
        }

        private string Markup(IHostService host, string markdown, FileAndType ft)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }
            return host.Markup(markdown, ft);
        }
    }
}
