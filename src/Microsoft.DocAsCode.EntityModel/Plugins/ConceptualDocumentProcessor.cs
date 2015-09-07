// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.EntityModel.YamlConverters;
    using Microsoft.DocAsCode.Plugins;

    [Export("Conceptual", typeof(IDocumentProcessor))]
    public class ConceptualDocumentProcessor : IDocumentProcessor
    {
        public Type ArticleModelType { get { return typeof(Dictionary<string, object>); } }

        public FileModel Load(FileAndType file)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    return new FileModel(file, MarkdownReader.ReadMarkdownAsConceptual(file.BaseDir, file.File))
                    {
                        Uids = new string[] { file.File },
                    };
                case DocumentType.Toc:
                    throw new NotImplementedException();
                case DocumentType.Resource:
                    return new FileModel(file, null);
                case DocumentType.Override:
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
            var content = (Dictionary<string, object>)model.Content;
            var markdown = (string)content["conceptual"];
            content["conceptual"] = host.Markup(markdown, model.FileAndType);
            model.File = Path.ChangeExtension(model.File, ".yml");
            return model;
        }

        public IEnumerable<FileModel> Postbuild(IEnumerable<FileModel> models, IHostService host)
        {
            return models;
        }
    }
}
