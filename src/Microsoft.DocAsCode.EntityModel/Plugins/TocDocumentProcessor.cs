// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IDocumentProcessor))]
    public class TocDocumentProcessor : IDocumentProcessor
    {
        public ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type == DocumentType.Article)
            {
                if ("toc.md".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingPriority.High;
                }
                if ("toc.yml".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingPriority.High;
                }
            }
            return ProcessingPriority.NotSupportted;
        }

        public FileModel Load(FileAndType file)
        {
            TocViewModel toc = null;
            if ("toc.md".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
            {
                toc = MarkdownTocReader.LoadToc(File.ReadAllText(Path.Combine(file.BaseDir, file.File)), file.File);
            }
            else if ("toc.yml".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
            {
                toc = YamlUtility.Deserialize<TocViewModel>(Path.Combine(file.BaseDir, file.File));
            }
            if (toc == null)
            {
                throw new NotSupportedException();
            }
            return new FileModel(file, toc)
            {
                Uids = new[] { file.File }.ToImmutableArray(),
            };
        }

        public SaveResult Save(FileModel model)
        {
            YamlUtility.Serialize(Path.Combine(model.BaseDir, model.File), model.Content);
            return new SaveResult
            {
                DocumentType = "TOC",
                ModelFile = model.File,
            };
        }

        public IEnumerable<FileModel> Prebuild(ImmutableArray<FileModel> models, IHostService host)
        {
            return models;
        }

        public void Build(FileModel model, IHostService host)
        {
            model.File = Path.ChangeExtension(model.File, ".yml");
            // todo : metadata.
        }

        public IEnumerable<FileModel> Postbuild(ImmutableArray<FileModel> models, IHostService host)
        {
            return models;
        }
    }
}
