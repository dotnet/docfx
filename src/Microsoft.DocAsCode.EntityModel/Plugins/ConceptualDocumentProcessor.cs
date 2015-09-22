// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IDocumentProcessor))]
    public class ConceptualDocumentProcessor : IDocumentProcessor
    {
        public ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type != DocumentType.Article)
            {
                return ProcessingPriority.NotSupportted;
            }
            if (".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
            {
                return ProcessingPriority.Normal;
            }
            return ProcessingPriority.NotSupportted;
        }

        public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            if (file.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }
            var content = MarkdownReader.ReadMarkdownAsConceptual(file.BaseDir, file.File);
            foreach (var item in metadata)
            {
                if (!content.ContainsKey(item.Key))
                {
                    content[item.Key] = item.Value;
                }
            }
            return new FileModel(
                file,
                content,
                serializer: YamlFormatter<Dictionary<string, object>>.Instance)
            {
                Uids = new string[] { file.File }.ToImmutableArray(),
            };
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
                DocumentType = "Conceptual",
                ModelFile = model.File,
                XRef = new string[0], // todo : find xref.
            };
        }

        public IEnumerable<FileModel> Prebuild(ImmutableArray<FileModel> models, IHostService host)
        {
            return models;
        }

        public void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article)
            {
                return;
            }
            var content = (Dictionary<string, object>)model.Content;
            var markdown = (string)content["conceptual"];
            content["conceptual"] = host.Markup(markdown, model.FileAndType);
            model.File = Path.ChangeExtension(model.File, ".yml");
        }

        public IEnumerable<FileModel> Postbuild(ImmutableArray<FileModel> models, IHostService host)
        {
            return models;
        }
    }
}
