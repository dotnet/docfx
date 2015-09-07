// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.IO;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.EntityModel.YamlConverters;
    using Microsoft.DocAsCode.Plugins;

    [Export("ManagedReference", typeof(IDocumentProcessor))]
    public class ManagedReferenceDocumentProcessor : IDocumentProcessor
    {
        public Type ArticleModelType { get { return typeof(PageViewModel); } }

        public FileModel Load(FileAndType file)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    var page = YamlUtility.Deserialize<PageViewModel>(Path.Combine(file.BaseDir, file.File));
                    return new FileModel(file, page)
                    {
                        Uids = (from item in page.Items select item.Uid).ToArray(),
                    };
                case DocumentType.Toc:
                    throw new NotImplementedException();
                case DocumentType.Resource:
                    return new FileModel(file, null);
                case DocumentType.Override:
                    var overrides = MarkdownReader.ReadMarkdownAsOverride(file.BaseDir, file.File);
                    return new FileModel(file, overrides)
                    {
                        // todo : strong type
                        Uids = (from Dictionary<string, object> item in (List<Dictionary<string, object>>)overrides["items"]
                                select (string)item["uid"]).ToArray()
                    };
                default:
                    throw new NotSupportedException();
            }
        }

        public void Save(FileModel model)
        {
            switch (model.Type)
            {
                case DocumentType.Article:
                    YamlUtility.Serialize(Path.Combine(model.BaseDir, model.File), model.Content);
                    break;
                case DocumentType.Toc:
                    throw new NotImplementedException();
                case DocumentType.Resource:
                    var originalFile = (FileAndType)model.Content;
                    if (model.FileAndType != originalFile)
                    {
                        File.Copy(Path.Combine(originalFile.BaseDir, originalFile.File), Path.Combine(model.BaseDir, model.File));
                    }
                    break;
                case DocumentType.Override:
                default:
                    throw new NotSupportedException();
            }
        }

        public IEnumerable<FileModel> Prebuild(IEnumerable<FileModel> models, IHostService host)
        {
            return models;
        }

        public FileModel BuildArticle(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article)
            {
                return model;
            }
            var page = (PageViewModel)model.Content;
            foreach (var item in page.Items)
            {
                BuildItem(host, item, model.FileAndType);
            }
            return model;
        }

        public IEnumerable<FileModel> Postbuild(IEnumerable<FileModel> models, IHostService host)
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
